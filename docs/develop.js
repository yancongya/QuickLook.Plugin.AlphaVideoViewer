const language = document.getElementById('language');
const menu = document.getElementById('menu');
const nav = document.querySelector('.desktop-nav');
let english = false;

document.querySelectorAll('[data-en]').forEach(element => {
  element.dataset.zh = element.innerHTML;
});
language.addEventListener('click', () => {
  english = !english;
  document.documentElement.lang = english ? 'en' : 'zh-CN';
  document.querySelectorAll('[data-en]').forEach(element => {
    element.innerHTML = english ? element.dataset.en : element.dataset.zh;
  });
  language.textContent = english ? 'EN' : 'CN';
  language.setAttribute('aria-label', english ? '切换到中文' : 'Switch to English');
});
menu.addEventListener('click', () => {
  const open = nav.classList.toggle('open');
  menu.setAttribute('aria-expanded', String(open));
});
document.addEventListener('keydown', event => {
  if (event.key === 'Escape') {
    nav.classList.remove('open');
    menu.setAttribute('aria-expanded', 'false');
  }
});
