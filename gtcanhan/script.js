/* ====== Utility ====== */
const $ = (q, el=document) => el.querySelector(q);
const $$ = (q, el=document) => Array.from(el.querySelectorAll(q));

/* ====== Theme Toggle (dark / light) ====== */
(function themeInit(){
  const root = document.documentElement;
  const saved = localStorage.getItem("theme");
  if (saved === "light") root.classList.add("light");
  const btn = $("#themeToggle");
  btn?.addEventListener("click", () => {
    root.classList.toggle("light");
    localStorage.setItem("theme", root.classList.contains("light") ? "light" : "dark");
  });
})();

/* ====== Mobile Drawer ====== */
(function drawer(){
  const toggle = $("#menuToggle");
  const drawer = $("#drawer");
  toggle?.addEventListener("click", () => {
    drawer.classList.toggle("open");
    toggle.setAttribute("aria-expanded", drawer.classList.contains("open") ? "true" : "false");
  });
  // Close drawer when clicking link
  $$("#drawer a").forEach(a => a.addEventListener("click", () => drawer.classList.remove("open")));
})();

/* ====== Smooth anchor offset for sticky header ====== */
(function smoothAnchors(){
  const headerH = $(".header")?.offsetHeight ?? 0;
  document.addEventListener("click", (e) => {
    const a = e.target.closest('a[href^="#"]');
    if (!a) return;
    const id = a.getAttribute("href").slice(1);
    const el = document.getElementById(id);
    if (!el) return;
    e.preventDefault();
    const y = el.getBoundingClientRect().top + window.scrollY - (headerH + 8);
    window.scrollTo({ top: y, behavior: "smooth" });
  });
})();

/* ====== Intersection Reveal ====== */
(function revealOnScroll(){
  const IO = new IntersectionObserver((entries)=>{
    entries.forEach(e=>{
      if(e.isIntersecting){
        e.target.classList.add("revealed");
        IO.unobserve(e.target);
      }
    });
  }, { threshold: .15 });
  $$("[data-reveal]").forEach(el => IO.observe(el));
})();

/* ====== Typed text effect ====== */
(function typedEffect(){
  const el = $("#typed");
  if (!el) return;
  // TODO: thay list job title theo ý bạn
  const items = [
    "Tester Leader",
    "Automation Enthusiast",
    "Frontend Tinkerer",
    "Trading Tool Builder"
  ];
  let i = 0, j = 0, typing = true;
  function tick(){
    const word = items[i];
    if (typing){
      el.textContent = word.slice(0, j++);
      if (j <= word.length){ return setTimeout(tick, 60); }
      typing = false; return setTimeout(tick, 1200);
    } else {
      el.textContent = word.slice(0, j--);
      if (j >= 0){ return setTimeout(tick, 30); }
      typing = true; i = (i+1) % items.length; return setTimeout(tick, 120);
    }
  }
  tick();
})();

/* ====== Counter when visible ====== */
(function counters(){
  const els = $$("[data-counter]");
  if (!els.length) return;
  const IO = new IntersectionObserver((entries)=>{
    entries.forEach(e=>{
      if (!e.isIntersecting) return;
      const el = e.target;
      const target = Number(el.dataset.counter || "0");
      let cur = 0;
      const step = Math.max(1, Math.ceil(target / 60));
      const timer = setInterval(()=>{
        cur += step;
        if (cur >= target){ cur = target; clearInterval(timer); }
        el.textContent = cur.toLocaleString("vi-VN");
      }, 16);
      IO.unobserve(el);
    });
  }, { threshold: .5 });
  els.forEach(el => IO.observe(el));
})();

/* ====== 3D Tilt (cards) ====== */
(function tilt(){
  const cards = $$("[data-tilt]");
  cards.forEach(card=>{
    let rect;
    card.addEventListener("pointermove", (e)=>{
      rect = rect || card.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width;
      const y = (e.clientY - rect.top) / rect.height;
      const rx = (y - .5) * -10;
      const ry = (x - .5) *  14;
      card.style.transform = `perspective(900px) rotateX(${rx}deg) rotateY(${ry}deg) translateZ(0)`;
    });
    card.addEventListener("pointerleave", ()=> card.style.transform = "");
  });
})();

/* ====== Contact: copy to clipboard & mailto submit ====== */
(function copyChips(){
  document.addEventListener("click", (e)=>{
    const btn = e.target.closest(".chip-action");
    if(!btn) return;
    const text = btn.getAttribute("data-copy");
    navigator.clipboard?.writeText(text).then(()=>{
      const old = btn.textContent;
      btn.textContent = "Copied!";
      setTimeout(()=> btn.textContent = old, 1200);
    });
  });
})();

function submitContact(ev){
  ev.preventDefault();
  const name = $("#name").value.trim();
  const email = $("#email").value.trim();
  const message = $("#message").value.trim();
  if (!name || !email || !message) return false;
  const subject = encodeURIComponent(`Liên hệ từ ${name}`);
  const body = encodeURIComponent(`${message}\n\n— ${name} (${email})`);
  // TODO: thay email nhận
  window.location.href = `mailto:tuan@example.com?subject=${subject}&body=${body}`;
  return false;
}

/* ====== Footer year ====== */
$("#year").textContent = new Date().getFullYear();
