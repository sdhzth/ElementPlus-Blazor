const { chromium } = require('playwright');
const fs = require('fs');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const BASE = process.argv[2] || 'http://127.0.0.1:5199';
const OUT = process.argv[3] || 'E:\\电子病历\\Element-Blazor-master\\demo\\shots';

const ROUTES = [
  ['home', '/', '组件演示中心'],
  ['basic', '/basic', '基础组件'],
  ['form', '/form', '表单组件'],
  ['form2', '/form2', '表单进阶组件'],
  ['data', '/data', '数据展示组件'],
  ['layout', '/layout', '布局与容器组件'],
  ['misc', '/misc', '交互与虚拟化组件'],
  ['feedback', '/feedback', '反馈组件'],
  ['nav', '/nav', '导航组件'],
  ['grow', '/grow', 'Grow'],
  ['x', '/x', 'AI / X'],
  ['all', '/all', '全部组件'],
];

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch({
    executablePath: EDGE, headless: true,
    args: ['--no-sandbox', '--disable-dev-shm-usage'],
  });

  const results = [];
  const allErrors = [];

  for (const [name, route, keyword] of ROUTES) {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const errors = [];
    page.on('console', m => { if (m.type() === 'error') errors.push('[console] ' + m.text()); });
    page.on('pageerror', e => errors.push('[pageerror] ' + String(e && e.message)));

    let ok = false, title = null, note = '';
    const t0 = Date.now();
    try {
      await page.goto(BASE + route, { waitUntil: 'domcontentloaded', timeout: 40000 });
      await page.waitForFunction(
        (kw) => {
          const h = document.querySelector('h1.demo-page-title');
          return h && h.textContent.includes(kw);
        },
        keyword,
        { timeout: 45000 }
      );
      await page.waitForTimeout(2200);
      title = (await page.textContent('h1.demo-page-title')).trim();
      ok = true;
    } catch (e) {
      note = e.message.split('\n')[0];
    }
    const ms = Date.now() - t0;

    const galleryCards = await page.locator('.demo-gallery-card').count().catch(() => 0);
    const galleryErrs = await page.locator('.demo-gallery-err').count().catch(() => 0);
    const sections = await page.locator('.demo-section').count().catch(() => 0);
    const errDetails = await page.evaluate(() => {
      const out = [];
      document.querySelectorAll('.demo-gallery-card').forEach(card => {
        const err = card.querySelector('.demo-gallery-err');
        if (err) {
          const head = card.querySelector('.demo-gallery-head span');
          out.push({ component: head ? head.textContent.trim() : '?', message: err.textContent.trim().slice(0, 180) });
        }
      });
      return out;
    }).catch(() => []);

    await page.screenshot({ path: OUT + '\\' + name + (ok ? '' : '_FAIL') + '.png', fullPage: true }).catch(() => {});

    results.push({ name, route, ok, title, ms, note, galleryCards, galleryErrs, sections, errDetails, errors: errors.slice(0, 6) });
    errors.forEach(e => allErrors.push('[' + name + '] ' + e));
    console.log((ok ? 'PASS ' : 'FAIL ') + name.padEnd(9) + route.padEnd(11) + ms + 'ms  ' + (title || note));
    errors.slice(0, 3).forEach(e => console.log('     ' + e.slice(0, 400)));
    await page.close().catch(() => {});
  }

  const failed = results.filter(r => !r.ok);
  fs.writeFileSync(OUT + '\\..\\test-result.json', JSON.stringify({ results, errorCount: allErrors.length, errors: allErrors.slice(0, 40) }, null, 2));
  console.log('\n=== SUMMARY: ' + (results.length - failed.length) + '/' + results.length + ' routes rendered ===');
  if (failed.length) console.log('FAILED: ' + failed.map(f => f.name).join(', '));
  await browser.close();
  process.exit(0);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
