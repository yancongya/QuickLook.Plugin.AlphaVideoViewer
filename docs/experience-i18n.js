// Terms for the illustrative controls; product claims live in the bilingual page.
const terms = {
'拼接素材':'Split video','普通视频':'Ordinary video','判断不明':'Unknown','明确普通':'Not alpha','近乎空白':'Empty output',
'几何预筛':'Geometry pre-filter','tkhd 几何初筛':'tkhd geometry','尺寸命中候选比例':'Candidate aspect ratio','两半都呈彩色':'Both halves colourful',
'交回 QuickLook':'Return to QuickLook','vapc 坐标有效':'Valid vapc coordinates','读取 rgbFrame / aFrame':'Read rgbFrame / aFrame',
'跳过像素门':'Skip pixel gate','元数据提供权威坐标':'Coordinates from metadata','直接送合成器':'Send to compositor','零猜测矩形对位':'Exact source rectangles',
'生成左右 / 上下候选':'Horizontal / vertical layouts','MF 解码 RGB32 帧确认':'MF checks RGB32 frames','插件接管预览':'Open plugin preview','前端逐帧选布局':'Player checks frame layouts',
'候选比例命中':'Geometry matches','解码失败或样本有歧义':'Decode failed or ambiguous','保留几何接管':'Keep geometry match','前端再逐帧自检':'Player checks frames',
'原生 VideoViewer':'Native VideoViewer','插件不接管':'Plugin releases file','找到近灰度遮罩与彩色内容':'Gray mask and colour found','Alpha 合成预览':'Alpha preview',
'进入候选布局检测':'Check candidate layouts','解码失败 / 采样不充分':'Decode failed / insufficient frames','保留宿主命中':'Keep host match','前端继续自检':'Continue player checks',
'近乎全透明':'Almost transparent','候选布局未得到可见结果':'No visible composite','插件内普通视频':'Plugin plain-video mode','原始帧继续播放':'Play original frames',
'遮罩强度 / ALPHA':'Mask strength / ALPHA','拆开展示':'Separate layers','合并图层':'Join layers','关闭预览':'Close preview',
'选择输入视频类型':'Choose input video type','选择自检结果':'Choose detection result','调节 Alpha 遮罩强度':'Adjust alpha strength',
'预览背景':'Preview background','棋盘格背景':'Checkerboard background','黑色背景':'Black background','白色背景':'White background',
'棋盘格':'Checkerboard','黑色':'Black','白色':'White','自定义背景颜色':'Custom background colour','自定义预览背景颜色':'Choose background colour',
'缩放预览画面':'Preview zoom','复位缩放与平移':'Reset zoom and pan','可平移缩放的透明合成样例':'Transparent sample: pan and zoom',
'灰度 Alpha 遮罩与 RGB 相乘得到的透明画面':'RGB pixels with alpha opacity','Alpha 源矩形':'Alpha source rectangle',
'RGB 源矩形':'RGB source rectangle','RGB 与 Alpha 合成输出':'Composited RGB and alpha','灰度 Alpha 遮罩像素':'Grayscale alpha pixels',
'RGB 颜色像素':'RGB colour pixels','将 RGB 与 Alpha 逐像素相乘后的透明结果':'Pixel-by-pixel RGB and alpha result','Alpha 遮罩强度':'Alpha mask strength',
'示意参数':'Illustrative values'
};
const reverse = Object.fromEntries(Object.entries(terms).map(([zh,en])=>[en,zh]));
export function localizeExperience(root, english) {
 if (!root) return;
 const translate=value=>{const key=reverse[value]||value;return english?(terms[key]||value):key;};
 const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT);
 let node;
 while((node=walker.nextNode())) {const value=node.nodeValue.trim(); if(value)node.nodeValue=node.nodeValue.replace(value,translate(value));}
 for(const element of root.querySelectorAll('[aria-label],[title]'))for(const attr of ['aria-label','title'])if(element.hasAttribute(attr))element.setAttribute(attr,translate(element.getAttribute(attr)));
}
