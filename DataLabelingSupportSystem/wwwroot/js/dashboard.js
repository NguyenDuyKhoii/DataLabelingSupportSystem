(function () {
  const btn = document.querySelector('[data-dash-toggle]');
  const sidebar = document.querySelector('.dash-sidebar');
  const overlay = document.querySelector('.dash-overlay');

  if (!btn || !sidebar || !overlay) return;

  function open() {
    sidebar.classList.add('open');
    overlay.classList.add('show');
  }
  function close() {
    sidebar.classList.remove('open');
    overlay.classList.remove('show');
  }

  btn.addEventListener('click', open);
  overlay.addEventListener('click', close);

  // close when click link on mobile
  sidebar.addEventListener('click', (e) => {
    const a = e.target.closest('a');
    if (a && window.innerWidth < 992) close();
  });
})();
