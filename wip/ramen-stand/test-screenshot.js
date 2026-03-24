const puppeteer = require('puppeteer');
const path = require('path');

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

(async () => {
  const browser = await puppeteer.launch({
    headless: true,
    executablePath: '/usr/bin/chromium-browser',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  
  const page = await browser.newPage();
  await page.setViewport({ width: 460, height: 900 });
  
  const htmlPath = 'file://' + path.resolve(__dirname, 'index.html');
  await page.goto(htmlPath);
  
  // タイトル画面で「はじめから」をクリック
  await page.evaluate(() => {
    const buttons = Array.from(document.querySelectorAll('button'));
    const btn = buttons.find(b => b.textContent.includes('はじめから'));
    if (btn) btn.click();
  });
  await sleep(1000);
  
  // 「営業開始」をクリック
  await page.evaluate(() => {
    const buttons = Array.from(document.querySelectorAll('button'));
    const btn = buttons.find(b => b.textContent.includes('営業開始'));
    if (btn) btn.click();
  });
  await sleep(15000); // 客が来て食事して表情が変わるまで待つ
  
  // スクリーンショット撮影
  await page.screenshot({ path: 'test-result.png', fullPage: true });
  
  console.log('Screenshot saved to test-result.png');
  
  // 客の表情を確認
  const emotions = await page.evaluate(() => {
    const customers = Array.from(document.querySelectorAll('.customer-face'));
    return customers.map(c => c.textContent);
  });
  console.log('Customer emotions:', emotions);
  
  await browser.close();
})();
