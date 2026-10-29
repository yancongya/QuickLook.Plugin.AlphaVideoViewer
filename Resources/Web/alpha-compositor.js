// 极简 alpha 拼接视频合成器：同一帧纹理里取两个任意源矩形，rgb × alpha 直出带 alpha 的画布。
// 只处理「两个矩形」这一件事，播放控制（seek/loop/音量/自适应）由页面持有。
// 之所以不用现成库：腾讯 VAP 的 mask 恒为主区 1/2 分辨率，主流库假设两块同长宽比，无法表达。

var VS = [
    'attribute vec2 aPos;',
    'varying vec2 vUv;',
    'void main() {',
    '  vUv = vec2(aPos.x * 0.5 + 0.5, 0.5 - aPos.y * 0.5);', // 翻转 V：以图像左上角为 (0,0)
    '  gl_Position = vec4(aPos, 0.0, 1.0);',
    '}'
].join('\n');

var FS = [
    'precision mediump float;',
    'uniform sampler2D uTex;',
    'uniform vec4 uRgb;',    // x, y, w, h（归一化到帧）
    'uniform vec4 uAlpha;',
    'uniform float uFlipR;', // 1 = 水平镜像（旋转元数据）
    'uniform float uFlipV;', // 1 = 上下翻转
    'varying vec2 vUv;',
    'vec2 mapRect(vec4 r, vec2 p) {',
    '  return vec2(r.x + p.x * r.z, r.y + p.y * r.w);',
    '}',
    'void main() {',
    '  vec2 p = vUv;',
    '  p.x = mix(p.x, 1.0 - p.x, uFlipR);',
    '  p.y = mix(p.y, 1.0 - p.y, uFlipV);',
    '  vec3 c = texture2D(uTex, mapRect(uRgb, p)).rgb;',
    '  float a = texture2D(uTex, mapRect(uAlpha, p)).r;',
    '  gl_FragColor = vec4(c * a, a);', // 预乘 alpha，配合 ONE/ONE_MINUS_SRC_ALPHA
    '}'
].join('\n');

function compile(gl, type, src) {
    var sh = gl.createShader(type);
    gl.shaderSource(sh, src);
    gl.compileShader(sh);
    if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS))
        throw new Error('shader: ' + gl.getShaderInfoLog(sh));
    return sh;
}

export default function createCompositor(options) {
    var video = options.video;
    var rects = options.rects;      // { rgb: [x,y,w,h], alpha: [x,y,w,h] }，帧归一化
    var outSize = options.outSize;  // { w, h } 内容像素尺寸，决定画布长宽比
    var maxSide = options.maxSide || 1280;

    var canvas = document.createElement('canvas');
    canvas.id = 'glCanvas';
    canvas.style.cssText = 'position:absolute;inset:0;margin:auto;max-width:100%;max-height:100%;display:block;';
    options.container.appendChild(canvas);

    var gl = canvas.getContext('webgl', { alpha: true, premultipliedAlpha: true, antialias: true });
    if (!gl) throw new Error('WebGL unavailable');

    var prog = gl.createProgram();
    gl.attachShader(prog, compile(gl, gl.VERTEX_SHADER, VS));
    gl.attachShader(prog, compile(gl, gl.FRAGMENT_SHADER, FS));
    gl.linkProgram(prog);
    if (!gl.getProgramParameter(prog, gl.LINK_STATUS))
        throw new Error('link: ' + gl.getProgramInfoLog(prog));
    gl.useProgram(prog);

    var buf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    var aPos = gl.getAttribLocation(prog, 'aPos');
    gl.enableVertexAttribArray(aPos);
    gl.vertexAttribPointer(aPos, 2, gl.FLOAT, false, 0, 0);

    var tex = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

    gl.enable(gl.BLEND);
    gl.blendFunc(gl.ONE, gl.ONE_MINUS_SRC_ALPHA);

    var u = {
        tex: gl.getUniformLocation(prog, 'uTex'),
        rgb: gl.getUniformLocation(prog, 'uRgb'),
        alpha: gl.getUniformLocation(prog, 'uAlpha'),
        flipR: gl.getUniformLocation(prog, 'uFlipR'),
        flipV: gl.getUniformLocation(prog, 'uFlipV')
    };
    gl.uniform1i(u.tex, 0);
    gl.uniform4fv(u.rgb, rects.rgb);
    gl.uniform4fv(u.alpha, rects.alpha);
    gl.uniform1f(u.flipR, rects.flipH ? 1 : 0);
    gl.uniform1f(u.flipV, rects.flipV ? 1 : 0);

    var raf = 0, destroyed = false, lastFrame = -1, active = true;

    function resize() {
        var vw = video.videoWidth, vh = video.videoHeight;
        if (!vw || !vh) return;
        var scale = Math.min(1, maxSide / Math.max(outSize.w, outSize.h));
        var w = Math.max(1, Math.round(outSize.w * scale));
        var h = Math.max(1, Math.round(outSize.h * scale));
        if (canvas.width !== w || canvas.height !== h) {
            canvas.width = w;
            canvas.height = h;
            gl.viewport(0, 0, w, h);
        }
    }

    function renderOnce() {
        resize();
        if (video.readyState < 2 || video.videoWidth <= 0) return false;
        var stamp = video.currentTime + ':' + video.videoWidth;
        if (stamp !== lastFrame || !video.paused) {
            lastFrame = stamp;
            try { gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, video); }
            catch (e) { /* 帧尚未就绪 */ }
        }
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
        return true;
    }

    function draw() {
        if (destroyed) return;
        if (active) renderOnce();
        raf = requestAnimationFrame(draw);
    }
    resize();
    raf = requestAnimationFrame(draw);

    return {
        canvas: canvas,
        setRects: function (next) {
            gl.uniform4fv(u.rgb, next.rgb);
            gl.uniform4fv(u.alpha, next.alpha);
            gl.uniform1f(u.flipR, next.flipH ? 1 : 0);
            gl.uniform1f(u.flipV, next.flipV ? 1 : 0);
        },
        setSize: function (w, h) {
            if (!(w > 0) || !(h > 0)) return;
            outSize = { w: w, h: h };
            resize();
        },
        setActive: function (on) {
            active = !!on;
            if (active) lastFrame = -1;   // 恢复时强制重新上传一帧
        },
        // 同步渲染一帧并回读像素：只给自检 / 自动化测试用
        readback: function () {
            var wasActive = active;
            active = true;
            var drew = renderOnce();
            active = wasActive;
            if (!drew) return null;
            var w = canvas.width, h = canvas.height;
            var buf = new Uint8Array(w * h * 4);
            gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, buf);
            return { w: w, h: h, data: buf };
        },
        destroy: function () {
            destroyed = true;
            cancelAnimationFrame(raf);
            gl.deleteTexture(tex);
            gl.deleteBuffer(buf);
            gl.deleteProgram(prog);
            if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
        }
    };
}
