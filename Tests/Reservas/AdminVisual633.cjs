// Sólo UI y solicitudes inválidas: no crea ni modifica registros de negocio.
const {chromium}=require(process.env.UGP_PLAYWRIGHT_MODULE || 'playwright-core');
const fs=require('node:fs'), path=require('node:path');
const out=path.resolve('artifacts/admin633-visual');fs.mkdirSync(out,{recursive:true});
const signal=path.join(out,'reload.signal');
const results={checks:[],resources:[],failures:[],disconnect:[],errors:[],screenshots:[]};let phase='before';
function record(ok,label){results.checks.push({phase,result:ok?'PASA':'FALLA',label});console.log((ok?'PASA: ':'FALLA: ')+label);}
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:false,chromiumSandbox:true});results.browser=await browser.version();
 const context=await browser.newContext({viewport:{width:1440,height:1000},ignoreHTTPSErrors:false});const page=await context.newPage();
 page.on('response',r=>{const u=new URL(r.url());if(/\.(css|js)$/.test(u.pathname))results.resources.push({phase,path:u.pathname,status:r.status(),mime:r.headers()['content-type']});if(u.pathname==='/_blazor/disconnect')results.disconnect.push({phase,status:r.status()});});
 page.on('requestfailed',r=>{const u=new URL(r.url());results.failures.push({phase,path:u.pathname,reason:r.failure()?.errorText});});
 page.on('pageerror',e=>results.errors.push({phase,type:e.name}));
 async function snap(name,width){await page.setViewportSize({width,height:1000});await page.screenshot({path:path.join(out,phase+'-'+name+'-'+width+'.png'),fullPage:true});results.screenshots.push(phase+'-'+name+'-'+width+'.png');const d=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));record(d.scroll<=d.width,name+' ancho '+width+' (contenido '+d.scroll+')');}
 try{
  await page.goto('https://localhost:7197/acceso-ugp');console.log('LOGIN PRIVADO en Edge. No se captura ni registra contraseña/TOTP.');
  await page.waitForURL('https://localhost:7197/administracion',{timeout:900000});await page.getByRole('heading',{name:/Administración UGP/i}).waitFor();
  if(!process.argv.includes('--after-only')){
  await snap('dashboard',1440);await page.goto('https://localhost:7197/administracion/localidades');await snap('localidades',360);
  results.beforeStyles=await page.evaluate(()=>({css:document.styleSheets.length,font:getComputedStyle(document.body).fontFamily,inputBackground:getComputedStyle(document.querySelector('input[type=number]')).backgroundColor}));
  fs.writeFileSync(path.join(out,'before.json'),JSON.stringify(results,null,2));console.log('ANTES CAPTURADO. Esperando señal de nueva compilación; conserva Edge abierto.');
  const started=Date.now();while(!fs.existsSync(signal)){if(Date.now()-started>1200000)throw Error('Espera de compilación agotada');await new Promise(r=>setTimeout(r,1000));}
  }
  phase='after';
  for(const [name,url] of [['dashboard','/administracion'],['eventos','/administracion/eventos'],['localidades','/administracion/localidades'],['editar-evento','/administracion/eventos/26'],['crear-evento','/administracion/eventos/nuevo']]){
   await page.goto('https://localhost:7197'+url);for(const width of [360,768,1440])await snap(name,width);
  }
  await page.getByLabel('Nombre',{exact:true}).fill('   ');await page.getByLabel('Lugar',{exact:true}).fill('Validación sin guardar');await page.getByRole('button',{name:'Guardar evento',exact:true}).click();
  record(page.url().endsWith('/nuevo'),'Entrada inválida no crea evento');record((await page.locator('body').innerText()).includes('El nombre es obligatorio.'),'Validación en español');await snap('validacion',360);
  await page.goto('https://localhost:7197/administracion/eventos/26');await page.getByLabel('Localidad global',{exact:true}).selectOption('0');await page.getByRole('button',{name:'Asociar sin modificar inventario',exact:true}).click();record((await page.locator('body').innerText()).includes('Selecciona una localidad.'),'Asociación inválida rechazada sin cambios');
  await page.goto('https://localhost:7197/administracion');await page.setViewportSize({width:360,height:1000});await page.getByText('Menú administrativo',{exact:true}).click();record(await page.getByRole('link',{name:'Eventos',exact:true}).isVisible(),'Menú administrativo móvil visible');await snap('menu',360);
  await page.goto('https://localhost:7197/administracion/localidades/21');await page.getByLabel('Nombre global',{exact:true}).focus();await page.keyboard.press('Tab');record(await page.getByLabel('Precio COP',{exact:true}).evaluate(e=>e===document.activeElement),'Orden de tabulación de formulario');
  await page.goto('https://localhost:7197/administracion/eventos/2147483647');record((await page.locator('body').innerText()).includes('Evento no encontrado.'),'Estado de error comprensible');await snap('error',768);
  await page.goto('https://localhost:7197/administracion');await page.getByRole('button',{name:'Cerrar sesión',exact:true}).click();const denied=await page.goto('https://localhost:7197/administracion/eventos');record(denied.status()===401,'Logout y protección de rutas');
 }catch(e){results.failure={type:e.name,message:e.message.slice(0,200)};console.log('PRUEBA DETENIDA: '+e.name);process.exitCode=2;}
 finally{fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(results,null,2));await browser.close();}
})();
