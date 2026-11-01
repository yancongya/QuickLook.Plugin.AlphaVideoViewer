import { initExperiences } from './experience.js';

const reduced = matchMedia('(prefers-reduced-motion: reduce)');
const motionButton = document.getElementById('motion');
const scenes = [...document.querySelectorAll('.scene')];
const hero = document.querySelector('.original-hero');
let motionPaused = reduced.matches;
const experiences = initExperiences({ reducedMotion: reduced.matches });
function syncScene(scene) {
  const playing = scene.dataset.visible === 'true' && scene.dataset.paused !== 'true' && !motionPaused && !document.hidden;
  experiences.setScenePlaying(scene, playing);
  const pauseButton = scene.querySelector('[data-action="pause"]');
  pauseButton.setAttribute('aria-pressed', String(scene.dataset.paused === 'true'));
  pauseButton.setAttribute('aria-label', document.documentElement.lang==='en' ? (scene.dataset.paused==='true'?'Play demo motion':'Pause demo motion') : (scene.dataset.paused==='true'?'播放当前演示动效':'暂停当前演示动效'));
  pauseButton.title = scene.dataset.paused === 'true' ? '播放动效' : '暂停动效';
  pauseButton.textContent = scene.dataset.paused === 'true' ? '▷' : 'Ⅱ';
}
const observer = new IntersectionObserver(entries => { for (const entry of entries) { entry.target.dataset.visible=String(entry.isIntersecting); syncScene(entry.target); } }, {threshold:.12});
for (const scene of scenes) {
  observer.observe(scene);
  scene.querySelector('[data-action="pause"]').addEventListener('click',()=>{scene.dataset.paused=String(scene.dataset.paused!=='true');syncScene(scene)});
  scene.querySelector('[data-action="replay"]').addEventListener('click',()=>{scene.dataset.paused='false';experiences.replay(scene);syncScene(scene)});
}
function syncMotion(){
  document.body.dataset.motion=motionPaused?'paused':'playing';
  motionButton.setAttribute('aria-pressed',String(motionPaused));
  motionButton.setAttribute('aria-label',document.documentElement.lang==='en'?(motionPaused?'Play page motion':'Pause page motion'):(motionPaused?'播放页面动效':'暂停页面动效'));
  motionButton.textContent=motionPaused?'▷':'Ⅱ';
  scenes.forEach(syncScene);
  experiences.setMotionPaused(motionPaused||document.hidden);
  syncFlow();
  if(motionPaused||document.hidden) document.querySelectorAll('video').forEach(video=>video.pause());
  hero.contentWindow?.postMessage({type:'motion',paused:motionPaused||document.hidden},location.origin);
}
hero.addEventListener('load',syncMotion);
motionButton.addEventListener('click',()=>{motionPaused=!motionPaused;syncMotion()});
reduced.addEventListener('change',()=>{motionPaused=reduced.matches;syncMotion()});
document.addEventListener('visibilitychange',syncMotion);
syncMotion();

// Section entrances and exits share the existing motion preference and pause control.
const revealGroups = [
  ['#top .main-content', '#top .original-hero'],
  ['#why .why-copy', '#why .pain-list', '#why .pain-turn'],
  ...[...document.querySelectorAll('#features .chapter')].map((_, index) => [
    `#features .chapter:nth-child(${index + 1}) .detail`,
    `#features .chapter:nth-child(${index + 1}) .scene`
  ]),
  ['#demo .demo-copy', '#demo .compositing-lab'],
  ['#docs .docs-copy', '#docs .docs-panel'],
  ['#install .install-steps', '#install .install-copy'],
  ['#dev .story-heading', '#dev .developer-workspace'],
  ['#download h2', '#download p', '#download .download-buttons', '#download .download-note'],
  ['.page-footer .wrap:first-child', '.page-footer .footer-fine']
];
if ('IntersectionObserver' in window) {
  const revealObserver = new IntersectionObserver(entries => {
    for (const entry of entries) {
      const element = entry.target;
      if (entry.isIntersecting) element.dataset.reveal = 'visible';
      else if (!element.contains(document.activeElement)) element.dataset.reveal = 'hidden';
    }
  }, { rootMargin: '0px 0px -7% 0px', threshold: 0.04 });
  for (const group of revealGroups) group.forEach((selector, index) => {
    const element = document.querySelector(selector);
    if (!element) return;
    element.dataset.reveal = 'hidden';
    element.dataset.revealSide = index % 2 ? 'right' : 'left';
    element.style.setProperty('--reveal-delay', `${index * 85}ms`);
    revealObserver.observe(element);
  });
  document.documentElement.classList.add('has-reveals');
  document.addEventListener('focusin', event => {
    event.target.closest('[data-reveal]')?.setAttribute('data-reveal', 'visible');
  });
}
const helpToggle = document.getElementById('help-toggle');
const helpPanel = document.getElementById('help-panel');
helpToggle.addEventListener('click', () => {
  helpPanel.hidden = !helpPanel.hidden;
  helpToggle.setAttribute('aria-expanded', String(!helpPanel.hidden));
});
document.addEventListener('keydown', event => {
  if (event.key === 'Escape') {
    if(helpPanel.contains(document.activeElement))helpToggle.focus();
    if(document.querySelector('.desktop-nav.open')?.contains(document.activeElement))document.getElementById('menu').focus();
    helpPanel.hidden = true;
    helpToggle.setAttribute('aria-expanded', 'false');
    document.querySelector('.desktop-nav').classList.remove('open');
    document.getElementById('menu').setAttribute('aria-expanded', 'false');
  }
});
const menu = document.getElementById('menu');
menu.addEventListener('click', () => {
  const expanded = document.querySelector('.desktop-nav').classList.toggle('open');
  menu.setAttribute('aria-expanded', String(expanded));
});
document.querySelectorAll('.desktop-nav a').forEach(link => link.addEventListener('click', () => {
  document.querySelector('.desktop-nav').classList.remove('open');
  menu.setAttribute('aria-expanded', 'false');
}));

const language = document.getElementById('language');
let english = false;
document.querySelectorAll('[data-en]').forEach(element => { element.dataset.zh = element.innerHTML; });
language.addEventListener('click', () => {
  english = !english;
  document.documentElement.lang = english ? 'en' : 'zh-CN';
  document.querySelectorAll('[data-en]').forEach(element => { element.innerHTML = english ? element.dataset.en : element.dataset.zh; });
  experiences.setLanguage(english);
  language.textContent = english ? 'EN' : 'CN';
  language.setAttribute('aria-label', english ? '切换到中文' : 'Switch to English');
  sendHeroLanguage();
  syncMotion();
  menu.setAttribute('aria-label',english?'Toggle navigation':'展开导航');
  helpToggle.setAttribute('aria-label',english?'Frequently asked questions':'常见问题');
  scenes.forEach(scene=>scene.querySelector('[data-action="replay"]').setAttribute('aria-label',english?'Replay demonstration':'重播演示'));
  updateNavigation();
});

// Preserve the live component's page-level Space gesture across its CSS isolation frame.
let heroInView=false, heroKeyHeld=false;
new IntersectionObserver(entries=>{heroInView=entries[0].isIntersecting},{threshold:.2}).observe(hero);
window.addEventListener('keydown',event=>{
 if(event.code!=='Space'||event.altKey||event.ctrlKey||event.metaKey||(!heroInView&&!heroKeyHeld))return;
 const active=document.activeElement;
 if(active?.matches('input,textarea,select,button,a,[contenteditable="true"]'))return;
 event.preventDefault();
 if(event.repeat||heroKeyHeld)return;
 heroKeyHeld=true;
 hero.contentWindow?.postMessage({type:'hero-key',phase:'down'},location.origin);
});
window.addEventListener('keyup',event=>{
 if(event.code!=='Space'||!heroKeyHeld)return;
 event.preventDefault();heroKeyHeld=false;
 hero.contentWindow?.postMessage({type:'hero-key',phase:'up'},location.origin);
});
window.addEventListener('blur',()=>{
 if(!heroKeyHeld)return;heroKeyHeld=false;
 hero.contentWindow?.postMessage({type:'hero-key',phase:'cancel'},location.origin);
});

function sendHeroLanguage(){hero.contentWindow?.postMessage({type:'language',english:document.documentElement.lang==='en'},location.origin);}
hero.addEventListener('load',sendHeroLanguage);
sendHeroLanguage();
function syncFlow(){
 document.querySelectorAll('.workflow-graphic,.story-motion').forEach(graphic=>{
  graphic.setAttribute('data-paused',String(motionPaused||document.hidden||graphic.dataset.visible!=='true'));
 });
}
const storyObserver=new IntersectionObserver(entries=>{
 entries.forEach(entry=>{entry.target.dataset.visible=String(entry.isIntersecting)});
 syncFlow();
},{threshold:.15});
document.querySelectorAll('.workflow-graphic,.story-motion').forEach(graphic=>storyObserver.observe(graphic));

const sectionLinks=[...document.querySelectorAll('.desktop-nav a[href^="#"]')];
const sectionGroups=[['top','top'],['why','why'],['features','features'],['demo','features'],['docs','docs'],['install','install'],['dev','dev'],['download','install']];
function activateNavigation(id){
 sectionLinks.forEach(link=>{
  const active=link.hash==='#'+id;link.classList.toggle('active',active);
  if(active)link.setAttribute('aria-current','location');else link.removeAttribute('aria-current');
 });
}
function updateNavigation(){
 const line=document.querySelector('.site-header').getBoundingClientRect().height+72;
 let active='top';
 for(const [id,group] of sectionGroups){const section=document.getElementById(id);if(section&&section.getBoundingClientRect().top<=line)active=group;}
 activateNavigation(active);
}
let navigationFrame=0;
addEventListener('scroll',()=>{if(!navigationFrame)navigationFrame=requestAnimationFrame(()=>{navigationFrame=0;updateNavigation();});},{passive:true});
addEventListener('resize',updateNavigation);
sectionLinks.forEach(link=>link.addEventListener('click',()=>activateNavigation(link.hash.slice(1))));
updateNavigation();

const docTabs=[...document.querySelectorAll('.docs-tabs [role="tab"]')];
function openDocTab(tab){
 docTabs.forEach(button=>{
  const selected=button===tab;button.setAttribute('aria-selected',String(selected));button.tabIndex=selected?0:-1;
  document.getElementById(button.getAttribute('aria-controls')).hidden=!selected;
 });
}
docTabs.forEach((tab,index)=>{
 tab.addEventListener('click',()=>openDocTab(tab));
 tab.addEventListener('keydown',event=>{
  let next=index;
  if(event.key==='ArrowRight')next=(index+1)%docTabs.length;
  else if(event.key==='ArrowLeft')next=(index+docTabs.length-1)%docTabs.length;
  else if(event.key==='Home')next=0;
  else if(event.key==='End')next=docTabs.length-1;
  else return;
  event.preventDefault();openDocTab(docTabs[next]);docTabs[next].focus();
 });
});
