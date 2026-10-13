const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const assert = require('node:assert/strict');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const errors = [], results = [];
  try {
    for (const viewport of [{width:1440,height:1000},{width:1024,height:768},{width:390,height:844}]) {
      const page = await browser.newPage({ viewport, reducedMotion: 'reduce' });
      page.on('pageerror', e => errors.push(String(e)));
      await page.goto(pathToFileURL(path.join(__dirname,'proposal.html')).href);
      await page.evaluate(() => document.fonts.ready);
      const check = await page.evaluate(() => ({
        cards:document.querySelectorAll('.boss').length,
        skills:document.querySelectorAll('.skills li').length,
        chapters:document.querySelectorAll('tbody tr').length,
        images:[...document.images].map(i=>({loaded:i.complete&&i.naturalWidth>0,alt:!!i.alt})),
        overflow:document.documentElement.scrollWidth>innerWidth+1,
        brokenAnchors:[...document.querySelectorAll('a[href^="#"]')].map(a=>a.getAttribute('href').slice(1)).filter(id=>!document.getElementById(id))
      }));
      assert.equal(check.cards,20); assert.equal(check.skills,60); assert.equal(check.chapters,20);
      assert.equal(check.images.length,6); assert(check.images.every(i=>i.loaded&&i.alt));
      assert.equal(check.overflow,false); assert.equal(check.brokenAnchors.length,0);
      const count = () => page.locator('.boss:visible').count();
      for (const [rank,n] of [['주요 보스',6],['중간보스',12],['시련 보스',2]]) {
        await page.selectOption('#rank',rank); assert.equal(await count(),n);
      }
      await page.selectOption('#rank','');
      await page.selectOption('#group','phantasia'); assert.equal(await count(),4);
      await page.selectOption('#rank','시련 보스'); assert.equal(await count(),1);
      await page.fill('#search','not-a-boss'); assert.equal(await count(),0);
      assert(await page.locator('#empty').isVisible());
      await page.selectOption('#rank',''); await page.selectOption('#group','');
      await page.fill('#search','다오스'); assert.equal(await count(),1);
      await page.fill('#search','mint-no-match');
      await page.evaluate(()=>{location.hash='ordo';});
      await page.waitForFunction(()=>!document.getElementById('ordo').hidden);
      assert.equal(await count(),20);
      await page.locator('#ordo details summary').click();
      assert.equal(await page.locator('#ordo details').getAttribute('open'),'');
      await page.evaluate(()=>{location.hash='';window.scrollTo(0,0)});
      const overview='review-'+viewport.width+'-overview.png';
      await page.screenshot({path:path.join(__dirname,overview)});
      await page.locator('#bosses').evaluate(el=>el.scrollIntoView({block:'start'}));
      const cards='review-'+viewport.width+'-cards.png';
      await page.screenshot({path:path.join(__dirname,cards)});
      results.push({viewport,...check,filterChecks:'passed',anchorUnhide:'passed',details:'passed',screenshots:[overview,cards]});
      await page.close();
    }
    assert.equal(errors.length,0);
    const result={status:'passed',scope:'offline proposal HTML, not Unity gameplay tests',browser:'Microsoft Edge / Playwright',results,errors};
    fs.writeFileSync(path.join(__dirname,'browser-review.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify({status:'passed',viewports:results.length,bosses:20,skills:60,images:6,errors:errors.length}));
  } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exitCode=1;});
