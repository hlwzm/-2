const { chromium } = require("playwright");
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  await page.goto("http://localhost:5000/", { waitUntil: "networkidle", timeout: 15000 });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: "D:\\CodexWorkSpace\\MyGame\\指尖江湖2\\Screenshots\\adminweb.png", fullPage: true });
  const title = await page.title();
  console.log("Title:", title);
  await browser.close();
})();
