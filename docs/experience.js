import { localizeExperience } from './experience-i18n.js';
const assetPaths = {
  rgb: './assets/illustrations/demo-rgb.png',
  mask: './assets/illustrations/demo-mask.png'
};

const panelMarkup = {
  space: [
    '<div class="xp-shell xp-space-demo" data-run="paused" data-demo-state="idle">',
    '<div class="xp-topline"><span><i></i> FILE EXPLORER</span><span>LOCAL PREVIEW</span></div>',
    '<div class="xp-workarea">',
    '<button class="xp-file" type="button" data-action="file-select" aria-pressed="false">',
    '<span class="xp-file-icon"><svg viewBox="0 0 40 48" aria-hidden="true"><path d="M7 2h17l11 11v31a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2Z" fill="#171717" stroke="#9b9b9b"/><path d="M24 2v11h11" fill="#343434" stroke="#9b9b9b"/><path d="M12 29c3-6 8-6 10 0s7 6 10 0" fill="none" stroke="#ededed" stroke-width="2"/></svg></span>',
    '<span class="xp-file-name">gift-effect.mp4</span><span class="xp-file-meta">MP4 · 1920 × 1080</span></button>',
    '<span class="xp-file-hint" data-file-hint>点击文件以选中</span>',
    '<div class="xp-preview" data-preview hidden><div class="xp-preview-head"><span><i></i><i></i><i></i> QuickLook</span><button type="button" data-action="preview-close" aria-label="关闭预览">×</button></div>',
    '<div class="xp-preview-frame"><div class="xp-transparent"><canvas data-art="result" aria-label="灰度 Alpha 遮罩与 RGB 相乘得到的透明画面"></canvas></div><span class="xp-preview-tag">ALPHA PREVIEW</span></div>',
    '<div class="xp-preview-foot"><span>gift-effect.mp4</span><span>00:04 / 00:08</span></div></div>',
    '</div><div class="xp-bottom"><button class="xp-space-key" type="button" data-action="space"><kbd>SPACE</kbd><span data-space-label>打开预览</span></button><p class="xp-status" data-live aria-live="polite">先选中视频文件</p></div>',
    '</div>'
  ].join(''),
  detection: [
    '<div class="xp-shell xp-detection" data-run="paused" data-kind="split">',
    '<div class="dx-switch" role="group" aria-label="选择输入视频类型">',
    '<button type="button" data-kind="vapc" aria-pressed="false">VAPC</button><button type="button" data-kind="split" aria-pressed="true">拼接素材</button><button type="button" data-kind="ordinary" aria-pressed="false">普通视频</button><button type="button" data-kind="unknown" aria-pressed="false">判断不明</button>',
    '</div><div class="dx-flow">',
    '<div class="dx-step" data-step="probe"><span>01 · HOST PROBE</span><strong data-value></strong><small data-detail></small></div>',
    '<i aria-hidden="true">→</i>',
    '<div class="dx-step" data-step="pixels"><span>02 · PIXEL CHECK</span><strong data-value></strong><small data-detail></small></div>',
    '<i aria-hidden="true">→</i>',
    '<div class="dx-step" data-step="route"><span>03 · PREVIEW ROUTE</span><strong data-value></strong><small data-detail></small></div>',
    '</div><div class="dx-outcome" role="status" aria-live="polite"><span data-outcome-title></span><p data-outcome-copy></p></div>',
    '</div>'
  ].join(''),
  compositor: [
    '<div class="xp-shell xp-compositor" data-run="paused" data-composed="true">',
    '<div class="xp-topline"><span>ONE FRAME TEXTURE</span><span>TWO SOURCE RECTANGLES</span></div>',
    '<div class="comp-layers">',
    '<div class="comp-layer"><span class="comp-label">ALPHA · aFrame</span><div class="comp-canvas comp-checker"><canvas data-art="mask" aria-label="Alpha 源矩形"></canvas><i class="comp-rect" aria-hidden="true"></i></div><small>0 → 1 OPACITY</small></div>',
    '<span class="comp-symbol" aria-hidden="true">×</span>',
    '<div class="comp-layer"><span class="comp-label">RGB · rgbFrame</span><div class="comp-canvas"><canvas data-art="rgb" aria-label="RGB 源矩形"></canvas><i class="comp-rect" aria-hidden="true"></i></div><small>COLOUR SOURCE</small></div>',
    '<span class="comp-symbol" aria-hidden="true">=</span>',
    '<div class="comp-layer comp-output"><span class="comp-label">OUTPUT · RGBA</span><div class="comp-canvas comp-checker"><canvas data-art="result" aria-label="RGB 与 Alpha 合成输出"></canvas></div><small data-comp-result>PREMULTIPLIED</small></div>',
    '</div><div class="comp-controls"><label for="scene-opacity"><span>遮罩强度 / ALPHA</span><output data-opacity-output>100%</output></label><input id="scene-opacity" type="range" min="0" max="100" value="100" data-opacity aria-label="调节 Alpha 遮罩强度"><button type="button" data-action="compose" aria-pressed="true">拆开展示</button></div>',
    '</div>'
  ].join(''),
  fallback: [
    '<div class="xp-shell xp-fallback" data-run="paused" data-kind="ordinary">',
    '<div class="fb-switch" role="group" aria-label="选择自检结果">',
    '<button type="button" data-kind="ordinary" aria-pressed="true">明确普通</button><button type="button" data-kind="alpha" aria-pressed="false">AlphaLike</button><button type="button" data-kind="unknown" aria-pressed="false">Unknown</button><button type="button" data-kind="empty" aria-pressed="false">近乎空白</button>',
    '</div><div class="fb-flow"><div class="fb-file"><span>INPUT</span><b>gift-effect.mp4</b><small>几何预筛</small></div><span class="fb-arrow" aria-hidden="true">→</span><div class="fb-gate" data-gate><span>FRAME PROBE</span><b data-gate-title></b><small data-gate-copy></small></div><span class="fb-arrow" aria-hidden="true">→</span><div class="fb-route" data-route><span>PREVIEW</span><b data-route-title></b><small data-route-copy></small></div></div>',
    '<p class="fb-note" data-live aria-live="polite"></p></div>'
  ].join(''),
  inspect: [
    '<div class="xp-shell xp-inspect" data-run="paused" data-bg="checker">',
    '<div class="inspect-view" data-inspect-view><canvas data-art="result" data-inspect-art aria-label="可平移缩放的透明合成样例"></canvas><aside class="inspect-info" data-info hidden><b>FRAME INFO</b><span>1920 × 1080 · 30 FPS</span><span>LAYOUT · SIDE BY SIDE</span><span>ALPHA · LEFT HALF</span><span>VIEW · COMPOSITED</span></aside></div>',
    '<div class="inspect-toolbar"><div class="inspect-swatches" role="group" aria-label="预览背景">',
    '<button type="button" data-bg="checker" aria-pressed="true" aria-label="棋盘格背景" title="棋盘格"><i class="swatch-checker"></i></button><button type="button" data-bg="black" aria-pressed="false" aria-label="黑色背景" title="黑色"><i class="swatch-black"></i></button><button type="button" data-bg="white" aria-pressed="false" aria-label="白色背景" title="白色"><i class="swatch-white"></i></button><label class="inspect-color" title="自定义背景颜色"><input type="color" data-bg-color value="#6f6f6f" aria-label="自定义预览背景颜色"></label></div>',
    '<label class="inspect-zoom"><span>ZOOM <output data-zoom-output>100%</output></span><input type="range" min="75" max="220" value="100" data-zoom aria-label="缩放预览画面"></label>',
    '<button class="inspect-info-toggle" type="button" data-action="info" aria-pressed="false">INFO</button><button class="inspect-reset" type="button" data-action="reset" title="复位缩放与平移" aria-label="复位缩放与平移">↺</button></div>',
    '<p class="inspect-status" data-live aria-live="polite">拖动查看细节 · Ctrl + 滚轮缩放 · 双击复位</p>',
    '</div>'
  ].join('')
};

const routeStates = {
  ordinary: {
    probe: ['tkhd 几何初筛', '尺寸命中候选比例'],
    pixels: ['NotAlpha', '两半都呈彩色'],
    route: ['交回 QuickLook', 'CanHandle = false'],
    title: ['普通视频走原生路径', 'Ordinary video stays native'],
    copy: ['解码像素明确不是拼接素材，QuickLook 内置 VideoViewer 接管播放。', 'Decoded pixels confirm this is not a split asset, so QuickLook’s built-in VideoViewer handles it.']
  },
  vapc: {
    probe: ['vapc 坐标有效', '读取 rgbFrame / aFrame'],
    pixels: ['跳过像素门', '元数据提供权威坐标'],
    route: ['直接送合成器', '零猜测矩形对位'],
    title: ['VAP v2 直读官方坐标', 'VAP v2 uses official coordinates'],
    copy: ['有效 vapc 元数据会跳过几何猜测与像素门，将两个归一化矩形直接交给合成器。', 'Valid vapc metadata bypasses geometry guessing and the pixel gate; normalized source rectangles go straight to the compositor.']
  },
  split: {
    probe: ['tkhd 几何初筛', '生成左右 / 上下候选'],
    pixels: ['AlphaLike', 'MF 解码 RGB32 帧确认'],
    route: ['插件接管预览', '前端逐帧选布局'],
    title: ['确认拼接后进入前端', 'Confirmed split enters the player'],
    copy: ['无 vapc 时先生成布局候选，再解码抽帧检查遮罩近灰度、内容有颜色与包围盒对齐。', 'Without vapc, candidate layouts are checked against decoded frames for a near-gray mask, colourful content, and aligned bounds.']
  },
  unknown: {
    probe: ['tkhd 几何初筛', '候选比例命中'],
    pixels: ['Unknown', '解码失败或样本有歧义'],
    route: ['保留几何接管', '前端再逐帧自检'],
    title: ['不确定时进入降级路径', 'Ambiguity keeps the fallback path'],
    copy: ['宿主不会把 Unknown 当成普通视频放行：保留几何初筛结果，再由前端逐帧自检或回退普通模式。', 'The host does not release Unknown as ordinary video: it keeps the geometry match for front-end frame checks or plain-video fallback.']
  }
};

const fallbackStates = {
  ordinary: {
    gate: ['NotAlpha', '两半都呈彩色'],
    route: ['原生 VideoViewer', '插件不接管'],
    note: ['已确认普通视频：CanHandle 返回 false，原始文件交给 QuickLook 内置播放器。', 'Confirmed ordinary video: CanHandle returns false and QuickLook’s built-in player receives the original file.']
  },
  alpha: {
    gate: ['AlphaLike', '找到近灰度遮罩与彩色内容'],
    route: ['Alpha 合成预览', '进入候选布局检测'],
    note: ['确认存在拼接特征：插件打开 WebView2 播放器并检查多个布局候选。', 'A split pattern is confirmed: the plugin opens its WebView2 player and checks layout candidates.']
  },
  unknown: {
    gate: ['Unknown', '解码失败 / 采样不充分'],
    route: ['保留宿主命中', '前端继续自检'],
    note: ['宿主保留几何初筛接管；前端逐帧自检，不能将这条路径描述为必然交回原生播放器。', 'The host keeps its geometry match for front-end frame checks; this path does not guarantee a return to the native player.']
  },
  empty: {
    gate: ['近乎全透明', '候选布局未得到可见结果'],
    route: ['插件内普通视频', '原始帧继续播放'],
    note: ['这是前端播放期间的显示回退：切换到未合成的原始视频画面，不是宿主的 CanHandle 回退。', 'This playback-time fallback shows the untouched video inside the plugin; it is not a host CanHandle fallback.']
  }
};

function loadImage(src) {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = reject;
    image.src = src;
  });
}

function contentBounds(image) {
  const probe = document.createElement('canvas');
  probe.width = image.naturalWidth;
  probe.height = image.naturalHeight;
  const context = probe.getContext('2d', { willReadFrequently: true });
  context.drawImage(image, 0, 0);
  const pixels = context.getImageData(0, 0, probe.width, probe.height).data;
  let left = probe.width, top = probe.height, right = 0, bottom = 0;
  for (let y = 0; y < probe.height; y += 2) {
    for (let x = 0; x < probe.width; x += 2) {
      const i = (y * probe.width + x) * 4;
      if (pixels[i + 3] > 8 && Math.max(pixels[i], pixels[i + 1], pixels[i + 2]) > 20) {
        left = Math.min(left, x);
        top = Math.min(top, y);
        right = Math.max(right, x);
        bottom = Math.max(bottom, y);
      }
    }
  }
  if (right <= left || bottom <= top) return { x: 0, y: 0, w: probe.width, h: probe.height };
  const padX = Math.round((right - left) * 0.025);
  const padY = Math.round((bottom - top) * 0.025);
  left = Math.max(0, left - padX);
  top = Math.max(0, top - padY);
  right = Math.min(probe.width - 1, right + padX);
  bottom = Math.min(probe.height - 1, bottom + padY);
  return { x: left, y: top, w: right - left + 1, h: bottom - top + 1 };
}

function preparePixels(rgbImage, maskImage) {
  const bounds = contentBounds(rgbImage);
  const width = 240;
  const height = Math.max(160, Math.round(width * bounds.h / bounds.w));
  const rgbCanvas = document.createElement('canvas');
  const maskCanvas = document.createElement('canvas');
  rgbCanvas.width = maskCanvas.width = width;
  rgbCanvas.height = maskCanvas.height = height;
  const rgbContext = rgbCanvas.getContext('2d', { willReadFrequently: true });
  const maskContext = maskCanvas.getContext('2d', { willReadFrequently: true });
  rgbContext.drawImage(rgbImage, bounds.x, bounds.y, bounds.w, bounds.h, 0, 0, width, height);
  // Both source images share coordinates; independent cropping would misalign the mask.
  const maskBounds = bounds;
  maskContext.drawImage(maskImage, maskBounds.x, maskBounds.y, maskBounds.w, maskBounds.h, 0, 0, width, height);
  return {
    width,
    height,
    rgb: rgbContext.getImageData(0, 0, width, height),
    mask: maskContext.getImageData(0, 0, width, height)
  };
}

function paintArt(canvas, pixels, mode, strength = 1) {
  if (!canvas || !pixels) return;
  canvas.width = pixels.width;
  canvas.height = pixels.height;
  const context = canvas.getContext('2d');
  const frame = mode === 'mask'
    ? new ImageData(new Uint8ClampedArray(pixels.mask.data), pixels.width, pixels.height)
    : new ImageData(new Uint8ClampedArray(pixels.rgb.data), pixels.width, pixels.height);
  if (mode === 'result') {
    const rgb = pixels.rgb.data;
    const mask = pixels.mask.data;
    for (let i = 0; i < frame.data.length; i += 4) {
      const alpha = Math.round(((mask[i] * 0.2126 + mask[i + 1] * 0.7152 + mask[i + 2] * 0.0722) / 255) * strength * 255);
      frame.data[i + 3] = Math.min(frame.data[i + 3], alpha);
    }
  }
  context.putImageData(frame, 0, 0);
}

function setBilingual(element, zh, en) {
  if (!element) return;
  element.innerHTML = zh;
  element.dataset.en = en;
}

function correctProductCopy() {
  const pain = document.querySelector('#compare .pain-row:last-child .pain-left span');
  setBilingual(pain,
    'Media Foundation 确认两半都呈彩色时，插件会放弃接管；解码失败或结果不明时，宿主仍保留几何初筛结果，再交给前端逐帧自检。',
    'If Media Foundation decodes both halves as colourful, the plugin releases the file. If decoding fails or the result is ambiguous, the host keeps its geometry pre-filter result for front-end frame checks.');
  setBilingual(document.querySelector('#why .pain-so'),
    'AlphaVideoViewer 先检查 VAP 元数据；没有元数据时，由宿主按几何初筛并尝试 Media Foundation 像素检查。明确的普通视频交回 QuickLook；解码失败或样本歧义时保留几何命中，再由前端逐帧自检。',
    'AlphaVideoViewer checks VAP metadata first. Without it, the host geometry-filters the file and attempts a Media Foundation pixel check. Confirmed ordinary video returns to QuickLook; decode failures and ambiguous samples keep the geometry match for a front-end frame check.');
  setBilingual(document.querySelector('#demo .demo-copy > p:nth-of-type(2)'),
    '有效 VAP v2 vapc 元数据提供 rgbFrame 与 aFrame 坐标，直接进入合成器。没有元数据时，几何初筛和像素自检逐步确认布局；明确为普通视频才交回 QuickLook，未知结果继续由前端自检。',
    'Valid VAP v2 vapc metadata supplies rgbFrame and aFrame directly to the compositor. Without metadata, geometry and pixel checks evaluate candidate layouts. Confirmed ordinary video returns to QuickLook; unknown results continue to front-end checks.');
  const featureFour = [...document.querySelectorAll('#features .chapter')][3];
  setBilingual(featureFour?.querySelector('h2'),
    '明确为普通视频，就交回 QuickLook',
    'Confirmed ordinary video goes back to QuickLook');
  setBilingual(featureFour?.querySelector('.text'),
    'Media Foundation 确认两半都呈彩色时，插件会放弃接管，交回 QuickLook 内置 VideoViewer。若解码失败或采样帧有歧义，宿主保留几何初筛结果，再由前端逐帧自检；若合成结果几乎全透明，则切回插件内的普通视频模式。',
    'When Media Foundation confirms both halves are colourful, the plugin releases the file to QuickLook’s built-in VideoViewer. If decoding fails or sampled frames are ambiguous, the host keeps its geometry pre-filter result for a front-end frame check. If compositing is nearly empty, the plugin switches to its own plain-video mode.');
  setBilingual(document.querySelector('#help-panel details:nth-of-type(2) p'),
    '宿主像素检查确认两半都呈彩色时，插件会把文件交回 QuickLook 内置 VideoViewer。解码失败或判断不明时，宿主可能保留几何命中，再交给前端自检。',
    'If the host pixel check confirms both halves are colourful, the plugin returns the file to QuickLook’s built-in VideoViewer. An ambiguous or failed decode can keep the host geometry match for front-end self-checking.');
  setBilingual(document.querySelector('#help-panel details:nth-of-type(3) p'),
    '支持 MP4、MOV 与 M4V。宿主先按几何初筛，再由解码帧或 VAP 元数据决定预览路径。',
    'MP4, MOV and M4V are supported. The host geometry-filters candidates, then decoded frames or VAP metadata determine the preview path.');
}

function createLab() {
  const old = document.querySelector('#demo .demo-media');
  if (!old) return null;
  const lab = document.createElement('div');
  lab.className = 'compositing-lab';
  lab.dataset.lab = '';
  lab.innerHTML = [
    '<div class="lab-topline"><span data-en="LIVE PIXEL MODEL">实时像素模型</span><span class="lab-pixel-state" data-lab-state aria-live="polite">正在读取灰度遮罩与 RGB 样例</span></div>',
    '<div class="lab-stage">',
    '<div class="lab-layer"><span class="lab-label" data-en="A · ALPHA">A · 透明度</span><div class="lab-image lab-checker"><canvas data-art="mask" aria-label="灰度 Alpha 遮罩像素"></canvas></div><span class="lab-meta" data-en="grayscale → opacity">灰度 → 不透明度</span></div>',
    '<span class="lab-operator" aria-hidden="true">×</span>',
    '<div class="lab-layer"><span class="lab-label" data-en="C · RGB">C · 颜色</span><div class="lab-image"><canvas data-art="rgb" aria-label="RGB 颜色像素"></canvas></div><span class="lab-meta" data-en="colour source">颜色来源</span></div>',
    '<span class="lab-operator lab-equals" aria-hidden="true">=</span>',
    '<div class="lab-layer lab-output-layer"><span class="lab-label" data-en="RGBA · OUTPUT">RGBA · 输出</span><div class="lab-image lab-checker"><canvas data-art="result" aria-label="将 RGB 与 Alpha 逐像素相乘后的透明结果"></canvas></div><span class="lab-meta" data-lab-result>RGB × Alpha</span></div>',
    '</div><div class="lab-controls"><label for="lab-opacity"><span data-en="Mask strength">遮罩强度</span><output id="lab-opacity-value" for="lab-opacity">100%</output></label>',
    '<input id="lab-opacity" data-lab-opacity type="range" min="0" max="100" value="100" aria-label="Alpha 遮罩强度">',
    '<button type="button" data-lab-toggle data-action="lab-toggle" aria-pressed="false"><span data-en="Separate layers">拆开展示</span></button></div>'
  ].join('');
  old.replaceWith(lab);
  return lab;
}

function updatePixels(root, pixels, opacity) {
  root.querySelectorAll('[data-art]').forEach(canvas => {
    paintArt(canvas, pixels, canvas.dataset.art, opacity);
  });
}

export function initExperiences({ reducedMotion = false } = {}) {
  correctProductCopy();
  const kinds = ['space','detection','compositor','fallback','inspect'];
  document.querySelectorAll('#features .scene object').forEach((object,index)=>{
    const panel=document.createElement('div');
    panel.className='experience'; panel.dataset.experience=kinds[index];
    panel.setAttribute('aria-label',object.getAttribute('aria-label')||kinds[index]);
    object.replaceWith(panel);
  });
  document.querySelectorAll('.experience[data-experience]').forEach(element => {
    element.innerHTML = panelMarkup[element.dataset.experience] || '';
  });
  const lab = createLab();
  const roots = [...document.querySelectorAll('.experience')];
  let english = false;
  let pixels = null;
  let assetsReady = false;
  let zoom = 1;
  let panX = 0;
  let panY = 0;
  let drag = null;
  let globalPaused = reducedMotion;

  function setMessage(root, zh, en) {
    const target = root.querySelector('[data-live]');
    if (!target) return;
    target.textContent = english ? en : zh;
  }

  function selectFile(root, selected) {
    const file = root.querySelector('.xp-file');
    file.setAttribute('aria-pressed', String(selected));
    root.classList.toggle('is-selected', selected);
    root.dataset.demoState = selected ? 'selected' : 'idle';
    root.querySelector('[data-file-hint]').textContent = selected ? (english ? 'FILE SELECTED' : '文件已选中') : (english ? 'Click the file to select it' : '点击文件以选中');
    setMessage(root, selected ? '文件已选中，现在按下方 Space 打开预览' : '先选中视频文件', selected ? 'File selected. Press Space below to open the preview.' : 'Select the video file first.');
  }

  function togglePreview(root, shouldOpen) {
    const preview = root.querySelector('[data-preview]');
    if (shouldOpen && root.querySelector('.xp-file').getAttribute('aria-pressed') !== 'true') {
      root.classList.remove('is-nudging');
      void root.offsetWidth;
      root.classList.add('is-nudging');
      setMessage(root, '先选中文件，QuickLook 才会响应空格', 'Select a file before opening QuickLook with Space.');
      return;
    }
    preview.hidden = !shouldOpen;
    root.dataset.demoState = shouldOpen ? 'open' : 'selected';
    root.querySelector('[data-action="space"]').setAttribute('aria-pressed', String(shouldOpen));
    root.querySelector('[data-space-label]').textContent = shouldOpen ? (english ? 'Close preview' : '关闭预览') : (english ? 'Open preview' : '打开预览');
    setMessage(root, shouldOpen ? 'QuickLook 预览已打开；再次按空格或关闭按钮收回' : '预览已关闭，文件仍保持选中', shouldOpen ? 'QuickLook preview is open. Press Space again or close it.' : 'Preview closed. The file remains selected.');
  }

  function renderDetection(root, kind) {
    const state = routeStates[kind];
    root.dataset.kind = kind;
    root.querySelectorAll('.dx-switch button').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.kind === kind)));
    ['probe', 'pixels', 'route'].forEach(key => {
      const step = root.querySelector('[data-step="' + key + '"]');
      step.querySelector('[data-value]').textContent = state[key][0];
      step.querySelector('[data-detail]').textContent = state[key][1];
      step.dataset.active = key === (kind === 'vapc' ? 'probe' : 'pixels') ? 'true' : 'false';
    });
    root.querySelector('[data-outcome-title]').textContent = state.title[english ? 1 : 0];
    root.querySelector('[data-outcome-copy]').textContent = state.copy[english ? 1 : 0];
    localizeExperience(root, english);
  }

  function renderFallback(root, kind) {
    const state = fallbackStates[kind];
    root.dataset.kind = kind;
    root.querySelectorAll('.fb-switch button').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.kind === kind)));
    root.querySelector('[data-gate-title]').textContent = state.gate[0];
    root.querySelector('[data-gate-copy]').textContent = state.gate[1];
    root.querySelector('[data-route-title]').textContent = state.route[0];
    root.querySelector('[data-route-copy]').textContent = state.route[1];
    root.querySelector('[data-gate]').dataset.state = kind;
    root.querySelector('[data-route]').dataset.state = kind;
    setMessage(root, state.note[0], state.note[1]);
    localizeExperience(root, english);
  }

  function setLabStatus() {
    if (!lab) return;
    const strength = Number(lab.querySelector('[data-lab-opacity]').value);
    lab.querySelector('[data-lab-opacity]').setAttribute('aria-valuetext', strength + '%');
    lab.querySelector('#lab-opacity-value').textContent = strength + '%';
    const separated = lab.dataset.separated === 'true';
    const button = lab.querySelector('[data-lab-toggle]');
    button.setAttribute('aria-pressed', String(separated));
    button.querySelector('span').textContent = separated ? (english ? 'Join layers' : '合并图层') : (english ? 'Separate layers' : '拆开展示');
    lab.querySelector('[data-lab-state]').textContent = assetsReady
      ? (english ? 'RGB pixels × grayscale opacity' : 'RGB 像素 × 灰度不透明度')
      : (english ? 'Loading local source pixels' : '正在读取本地源像素');
  }

  function resetInspect(root) {
    zoom = 1;
    panX = 0;
    panY = 0;
    const input = root.querySelector('[data-zoom]');
    input.value = '100';
    input.setAttribute('aria-valuetext', '100%');
    root.querySelector('[data-zoom-output]').textContent = '100%';
    const art = root.querySelector('[data-inspect-art]');
    art.style.transform = 'translate(0px, 0px) scale(1)';
    setMessage(root, '视图已复位 · 拖动查看细节 · Ctrl + 滚轮缩放', 'View reset. Drag to inspect; Ctrl + wheel to zoom.');
  }

  function setBackground(root, background, color) {
    root.dataset.bg = background; root.querySelector('.xp-inspect').dataset.bg = background;
    const view = root.querySelector('[data-inspect-view]');
    view.style.backgroundColor = background === 'custom' ? color : ''; view.style.backgroundImage = background === 'custom' ? 'none' : '';
    root.querySelectorAll('[data-bg]').forEach(button => {
      if (button instanceof HTMLButtonElement) button.setAttribute('aria-pressed', String(button.dataset.bg === background));
    });
    setMessage(root, '背景已切换，可在明暗底色上检查透明边缘', 'Background changed. Inspect transparency against light or dark.');
  }

  for (const root of roots) {
    if (root.dataset.experience === 'detection') renderDetection(root, 'split');
    if (root.dataset.experience === 'fallback') renderFallback(root, 'ordinary');
    if (root.dataset.experience === 'space') selectFile(root, false);
    if (root.dataset.experience === 'inspect') {
      root.querySelectorAll('[data-bg]').forEach(button => button.addEventListener('click', () => setBackground(root, button.dataset.bg)));
      root.querySelector('[data-bg-color]').addEventListener('input', event => setBackground(root, 'custom', event.target.value));
      root.querySelector('[data-zoom]').addEventListener('input', event => {
        zoom = Number(event.target.value) / 100;
        root.querySelector('[data-zoom-output]').textContent = event.target.value + '%';
        event.target.setAttribute('aria-valuetext', event.target.value + '%');
        const art = root.querySelector('[data-inspect-art]');
        art.style.transform = 'translate(' + panX + 'px, ' + panY + 'px) scale(' + zoom + ')';
      });
      const view = root.querySelector('[data-inspect-view]');
      view.addEventListener('pointerdown', event => {
        if (event.button !== 0) return;
        drag = { root, id: event.pointerId, x: event.clientX, y: event.clientY, panX, panY };
        view.setPointerCapture(event.pointerId);
      });
      view.addEventListener('pointermove', event => {
        if (!drag || drag.root !== root || drag.id !== event.pointerId) return;
        panX = drag.panX + event.clientX - drag.x;
        panY = drag.panY + event.clientY - drag.y;
        root.querySelector('[data-inspect-art]').style.transform = 'translate(' + panX + 'px, ' + panY + 'px) scale(' + zoom + ')';
      });
      view.addEventListener('pointerup', event => { if (drag?.root === root && drag.id === event.pointerId) drag = null; });
      view.addEventListener('pointercancel', () => { drag = null; });
      view.addEventListener('dblclick', () => resetInspect(root));
      view.addEventListener('wheel', event => {
        if (!event.ctrlKey) return;
        event.preventDefault();
        const input = root.querySelector('[data-zoom]');
        input.value = String(Math.max(75, Math.min(220, Number(input.value) + (event.deltaY < 0 ? 10 : -10))));
        input.dispatchEvent(new Event('input', { bubbles: true }));
      }, { passive: false });
    }
  }

  document.addEventListener('keydown', event => {
    const root=event.target.closest('[data-experience="space"]');
    if(!root || event.code!=='Space' || !event.target.matches('.xp-file')) return;
    event.preventDefault();
    if(!event.repeat) togglePreview(root,root.dataset.demoState!=='open');
  });
  document.addEventListener('keyup', event => {
    if(event.code==='Space' && event.target.matches('.xp-file')) event.preventDefault();
  });
  document.addEventListener('click', event => {
    const action = event.target.closest('[data-action]');
    if (action) {
      const root = action.closest('.experience');
      if (root) {
        if (action.dataset.action === 'file-select') selectFile(root, action.getAttribute('aria-pressed') !== 'true');
        if (action.dataset.action === 'space') togglePreview(root, root.dataset.demoState !== 'open');
        if (action.dataset.action === 'preview-close') togglePreview(root, false);
        if (action.dataset.action === 'compose') {
          const shell = root.querySelector('.xp-compositor'); const composed = shell.dataset.composed !== 'true'; shell.dataset.composed = String(composed); const separated = !composed;
          root.dataset.composed = String(!separated);
          action.setAttribute('aria-pressed', String(!separated));
          action.textContent = separated ? (english ? 'Join layers' : '合并图层') : (english ? 'Separate layers' : '拆开展示');
          root.querySelector('[data-comp-result]').textContent = separated ? 'SOURCE RECTANGLES' : 'PREMULTIPLIED';
        }
        if (action.dataset.action === 'info') {
          const info = root.querySelector('[data-info]');
          info.hidden = !info.hidden;
          action.setAttribute('aria-pressed', String(!info.hidden));
          setMessage(root, info.hidden ? '信息面板已关闭' : '信息面板已打开 · 当前数值为界面示意', info.hidden ? 'Info panel closed.' : 'Info panel open. Values are illustrative.');
        }
        if (action.dataset.action === 'reset') resetInspect(root);
      }
      const labToggle = action.closest('[data-lab-toggle]');
      if (labToggle && lab) {
        lab.dataset.separated = String(lab.dataset.separated !== 'true');
        setLabStatus();
      }
    }
    const kindButton = event.target.closest('[data-kind]');
    if (kindButton) {
      const root = kindButton.closest('.experience');
      if (root?.dataset.experience === 'detection') renderDetection(root, kindButton.dataset.kind);
      if (root?.dataset.experience === 'fallback') renderFallback(root, kindButton.dataset.kind);
    }
    const labOpacity = event.target.closest('[data-lab-opacity]');
    if (labOpacity && lab) setLabStatus();
  });

  function updateOpacity(value, scope) {
    const strength = Math.max(0, Math.min(1, Number(value) / 100));
    scope.querySelectorAll('[data-art="result"]').forEach(canvas => paintArt(canvas, pixels, 'result', strength));
  }
  document.addEventListener('input', event => {
    if (event.target.matches('[data-opacity]')) {
      const root = event.target.closest('.xp-compositor');
      root.querySelector('[data-opacity-output]').textContent = event.target.value + '%';
      event.target.setAttribute('aria-valuetext', event.target.value + '%');
      updateOpacity(event.target.value, root);
    }
    if (event.target.matches('[data-lab-opacity]')) { updateOpacity(event.target.value, lab); setLabStatus(); }
  });

  const labObserver = lab && 'IntersectionObserver' in window
    ? new IntersectionObserver(entries => {
      lab.dataset.visible = String(entries.some(entry => entry.isIntersecting));
      lab.dataset.run = lab.dataset.visible === 'true' && !globalPaused ? 'playing' : 'paused';
    }, { threshold: 0.15 })
    : null;
  if (labObserver) labObserver.observe(lab);
  setLabStatus();

  Promise.all([loadImage(assetPaths.rgb), loadImage(assetPaths.mask)]).then(([rgbImage, maskImage]) => {
    pixels = preparePixels(rgbImage, maskImage);
    assetsReady = true;
    document.querySelectorAll('.experience,[data-lab]').forEach(root => updatePixels(root, pixels, 1));
    if (lab) setLabStatus();
  }).catch(() => {
    const state = lab?.querySelector('[data-lab-state]');
    if (state) state.textContent = english ? 'Local sample image failed to load' : '本地样例图片加载失败';
  });

  return {
    setScenePlaying(scene, playing) {
      const root = scene.querySelector('.experience');
      if (root) root.dataset.run = playing ? 'playing' : 'paused';
    },
    replay(scene) {
      const root = scene.querySelector('.experience');
      if (!root) return;
      if(root.dataset.experience==='space'){selectFile(root,true);togglePreview(root,true);}
      if(root.dataset.experience==='detection')renderDetection(root,'split');
      if(root.dataset.experience==='fallback')renderFallback(root,'ordinary');
      if(root.dataset.experience==='inspect')resetInspect(root);
      if(root.dataset.experience==='compositor'){
        root.querySelector('.xp-compositor').dataset.composed='true';
        const button=root.querySelector('[data-action="compose"]');button.setAttribute('aria-pressed','true');button.textContent=english?'Separate layers':'拆开展示';
        const input=root.querySelector('[data-opacity]');input.value='100';input.dispatchEvent(new Event('input',{bubbles:true}));
      }
      root.dataset.replay = 'false';
      void root.offsetWidth;
      root.dataset.replay = 'true';
      window.setTimeout(() => { root.dataset.replay = 'false'; }, reducedMotion ? 1 : 720);
    },
    setMotionPaused(paused) {
      globalPaused = paused;
      if (lab) lab.dataset.run = !paused && lab.dataset.visible === 'true' ? 'playing' : 'paused';
    },
    setLanguage(value) {
      english = value;
      roots.forEach(root => {
        if (root.dataset.experience === 'detection') renderDetection(root, root.dataset.kind);
        if (root.dataset.experience === 'fallback') renderFallback(root, root.dataset.kind);
        if (root.dataset.experience === 'space') {
          const selected = root.querySelector('.xp-file').getAttribute('aria-pressed') === 'true';
          const open = root.dataset.demoState === 'open';
          root.querySelector('[data-file-hint]').textContent = selected ? (english ? 'FILE SELECTED' : '文件已选中') : (english ? 'Click the file to select it' : '点击文件以选中');
          root.querySelector('[data-space-label]').textContent = open ? (english ? 'Close preview' : '关闭预览') : (english ? 'Open preview' : '打开预览');
          setMessage(root, open ? 'QuickLook 预览已打开；再次按空格或关闭按钮收回' : selected ? '文件已选中，现在按下方 Space 打开预览' : '先选中视频文件', open ? 'QuickLook preview is open. Press Space again or close it.' : selected ? 'File selected. Press Space below to open the preview.' : 'Select the video file first.');
        }
        if (root.dataset.experience === 'inspect') setMessage(root, '拖动查看细节 · Ctrl + 滚轮缩放 · 双击复位', 'Drag to inspect · Ctrl + wheel to zoom · double-click to reset');
      });
      roots.forEach(root=>localizeExperience(root,english));
      if (lab) { setLabStatus(); localizeExperience(lab,english); }
    }
  };
}
