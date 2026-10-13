const {chromium}=require('playwright');
const fs=require('node:fs');
const path=require('node:path');
const {pathToFileURL}=require('node:url');
const assert=require('node:assert/strict');
(async()=>{
  const browser=await chromium.launch({channel:'msedge',headless:true});
  const errors=[],checks=[];
  try{
    for(const viewport of [{width:1440,height:1100},{width:1024,height:900},{width:390,height:844}]){
      const page=await browser.newPage({viewport});
      page.on('pageerror',e=>errors.push(e.message));
      await page.goto(pathToFileURL(path.join(__dirname,'proposal.html')).href);
      await page.evaluate(()=>document.fonts.ready);
      for(const id of ['feedback','facing','vfx','story']){
        await page.locator('[data-tab='+id+']').click();
        assert.equal(await page.locator('#'+id).isVisible(),true);
        const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1);
        assert.equal(overflow,false,`${id}: horizontal overflow at ${viewport.width}`);
        if(id==='feedback'){
          await page.locator('#play-xp').click();
          await page.waitForFunction(()=>document.querySelector('#xp-value').textContent==='15 / 100');
          assert.equal(await page.locator('#xp-level').textContent(),'Lv. 9');
          await page.locator('#play-mint').click();
          await page.waitForFunction(()=>document.querySelector('#xp-value').textContent==='52 / 100');
          assert.equal(await page.locator('#xp-level').textContent(),'Lv. 7');
          await page.locator('#show-damage').click();
          assert.equal(await page.locator('#damage').textContent(),'128');
        }
        if(id==='facing'){
          await page.locator('.direction.sw').click();
          assert.match(await page.locator('#dir-name').textContent(),/남서/);
          await page.locator('#turn-camera').click();
          assert.match(await page.locator('#camera-info').textContent(),/90°/);
          assert.equal(await page.locator('.direction[aria-pressed=true]').count(),1);
        }
        if(id==='vfx'){
          assert.equal(await page.locator('.vfx-board').evaluate(i=>i.complete&&i.naturalWidth>0),true);
          await page.locator('#play-motion').click();
          assert.match(await page.locator('#motion-test').getAttribute('class'),/playing/);
        }
        if(id==='story'){
          await page.locator('#next-story').click();
          assert.equal(await page.locator('#speaker').textContent(),'민트');
          await page.locator('#layout-story').click();
          assert.equal(await page.locator('#story-left').isVisible(),true);
          assert.equal(await page.locator('#story-right').isVisible(),false);
          const overflow=await page.locator('.dialogue').evaluate(e=>e.scrollWidth>e.clientWidth+1);
          assert.equal(overflow,false,'dialogue internal overflow');
          await page.locator('#reset-story').click();
          assert.equal(await page.locator('#speaker').textContent(),'크레스');
        }
        if(viewport.width===1440||viewport.width===390){
          const target=id==='vfx'?page.locator('.vfx-board'):page.locator('#'+id+' .stage');
          await target.screenshot({path:path.join(__dirname,`${id}-${viewport.width}.png`)});
        }
        checks.push({width:viewport.width,panel:id,passed:true});
      }
      await page.close();
    }
    assert.equal(errors.length,0);
    fs.writeFileSync(path.join(__dirname,'browser-validation.json'),JSON.stringify({checks,errors},null,2));
    console.log(JSON.stringify({panels:checks.length,errors:errors.length,result:'passed'}));
  }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
