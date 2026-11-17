// Prueba local explícita. Usa Playwright ya instalado; no instala ni guarda credenciales/cookies.
const { chromium } = require('C:/Program Files/WindowsApps/OpenAI.Codex_26.1103.7773.0_x64__2p2nqsd0c76g0/app/resources/cua_node/bin/node_modules/playwright-core');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve('artifacts/admin632-visual'); fs.mkdirSync(root,{recursive:true});
const report={cases:[],screenshots:[],httpErrors:[],jsErrors:0,consoleErrors:0,requestFailures:[],events:[]};
function check(ok,label){report.cases.push({result:ok?'PASA':'FALLA',label}); console.log((ok?'PASA: ':'FALLA: ')+label); if(!ok && !process.argv.includes('--finish'))throw Error(label);}
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:false,chromiumSandbox:true});
 const context=await browser.newContext({viewport:{width:1440,height:900},ignoreHTTPSErrors:false});
 const page=await context.newPage();
 page.on('pageerror',()=>report.jsErrors++);
 page.on('console',m=>{if(m.type()==='error')report.consoleErrors++;});
 page.on('response',r=>{if(r.status()>=400){const u=new URL(r.url());report.httpErrors.push({host:u.host,path:u.pathname,status:r.status()});}});
 page.on('requestfailed',r=>{const u=new URL(r.url());report.requestFailures.push({host:u.host,path:u.pathname});});
 try{
  await page.goto('https://localhost:7197/acceso-ugp');
  console.log('LOGIN PRIVADO: completa usuario, contraseña y TOTP directamente en Edge. No se captura el login.');
  await page.waitForURL('https://localhost:7197/administracion',{timeout:900000});
  await page.getByRole('heading',{name:/Administración UGP/i}).waitFor({state:'visible'});
  check(true,'Dashboard después de login privado con MFA');
  async function capture(label,width){await page.setViewportSize({width,height:900});await page.screenshot({path:path.join(root,label+'-'+width+'.png'),fullPage:true});report.screenshots.push(label+'-'+width+'.png');const dims=await page.evaluate(()=>({viewport:innerWidth,body:document.documentElement.scrollWidth}));check(dims.body<=dims.viewport+1,label+' sin desbordamiento a '+width+'px');}
  if(process.argv.includes('--finish')){
   await page.goto('https://localhost:7197/administracion/localidades/21');
   check(Number(await page.getByLabel('Precio COP',{exact:true}).inputValue())===10000,'Precio original conservado tras intento bloqueado');
   await page.getByLabel('Capacidad global por evento',{exact:true}).fill('11');await page.getByRole('button',{name:'Guardar localidad',exact:true}).click();
   check(await page.getByRole('alert').filter({hasText:'Localidad compartida'}).count()===1,'Capacidad compartida no editable');
   report.dimensions=[];
   for(const width of [360,768,1440]){
    await page.setViewportSize({width,height:900});
    const dims=await page.evaluate(()=>({viewport:innerWidth,body:document.documentElement.scrollWidth,overflow:[...document.querySelectorAll('h3 a')].filter(e=>e.getBoundingClientRect().right>innerWidth).length}));
    report.dimensions.push(dims);report.cases.push({result:dims.body<=width+1?'PASA':'FALLA',label:'Catálogo sin overflow '+width});
    await page.screenshot({path:path.join(root,'catalogo-final-'+width+'.png'),fullPage:true});report.screenshots.push('catalogo-final-'+width+'.png');
   }
   await page.goto('https://localhost:7197/administracion/localidades/21');check(Number(await page.getByLabel('Capacidad global por evento',{exact:true}).inputValue())===10,'GET nuevo conserva capacidad original');
   await page.getByLabel('Nombre global',{exact:true}).focus();await page.keyboard.press('Tab');check(await page.getByLabel('Precio COP',{exact:true}).evaluate(el=>el===document.activeElement),'Tab del nombre al precio');
   await page.setViewportSize({width:360,height:900});await page.getByRole('button',{name:/Menú/}).click();
   check(await page.getByRole('button',{name:/Menú/}).getAttribute('aria-expanded')==='true','Menú móvil abre mediante botón');
   await page.screenshot({path:path.join(root,'menu-360.png'),fullPage:true});report.screenshots.push('menu-360.png');
   await page.goto('https://localhost:7197/administracion');await page.getByRole('button',{name:'Cerrar sesión',exact:true}).click();
   const denied=await page.goto('https://localhost:7197/administracion/eventos');check(denied.status()===401,'Logout protege eventos HTTP 401');
   return;
  }
  for(const width of [360,768,1440])await capture('dashboard',width);
  await page.getByRole('link',{name:'Administrar eventos',exact:true}).click();
  check(await page.getByRole('heading',{level:1}).isVisible(),'Listado administrativo abierto');
  await page.goto('https://localhost:7197/administracion/eventos/nuevo');
  const marker='QA632-'+Date.now(); report.marker=marker;
  async function fillEvent(name){await page.getByLabel('Nombre',{exact:true}).fill(name);await page.getByLabel('Lugar',{exact:true}).fill('Laboratorio visual sintético');await page.getByLabel('Fecha (Colombia)',{exact:true}).fill('2030-05-01');await page.getByLabel('Hora (Colombia)',{exact:true}).fill('18:00');await page.getByLabel('Aforo total (se fija al crear)',{exact:true}).fill('20');}
  await fillEvent('   ');await page.getByRole('button',{name:'Guardar evento',exact:true}).click();
  check(page.url().endsWith('/nuevo'),'Nombre de espacios no crea evento');
  for(const width of [360,768,1440])await capture('validacion-evento',width);
  await fillEvent(marker+'-A');await page.getByRole('button',{name:'Guardar evento',exact:true}).click();await page.waitForURL(/\/administracion\/eventos\/\d+$/);
  const first=page.url();report.events.push(Number(first.split('/').pop()));
  check(await page.getByText('No hay localidades asociadas.',{exact:true}).isVisible(),'Estado vacío de localidades');
  await page.getByLabel('Lugar',{exact:true}).fill('Laboratorio visual editado');await page.getByRole('button',{name:'Guardar evento',exact:true}).click();
  await page.waitForLoadState('domcontentloaded');check(await page.getByLabel('Lugar',{exact:true}).inputValue()==='Laboratorio visual editado','Edición persiste después de guardar');
  await page.getByRole('link',{name:'Ver detalle público',exact:true}).click();check(await page.getByText(marker+'-A',{exact:true}).count()>0,'Nombre reflejado en detalle público');
  await page.goto('https://localhost:7197/administracion/localidades');
  // Reutilizar el tipo sintético creado por la prueba de concurrencia, sin modificarlo.
  await page.goto(first);await page.getByLabel('Localidad global',{exact:true}).selectOption('21');
  await page.getByRole('button',{name:'Asociar sin modificar inventario',exact:true}).click();await page.waitForLoadState('domcontentloaded');
  check(await page.getByRole('link',{name:'Consultar tipo global',exact:true}).count()===1,'Asociación visible de localidad existente');
  for(const width of [360,768,1440])await capture('evento-asociacion',width);
  await page.goto('https://localhost:7197/administracion/eventos/nuevo');await fillEvent(marker+'-B');await page.getByRole('button',{name:'Guardar evento',exact:true}).click();await page.waitForURL(/\/administracion\/eventos\/\d+$/);
  report.events.push(Number(page.url().split('/').pop()));await page.getByLabel('Localidad global',{exact:true}).selectOption('21');await page.getByRole('button',{name:'Asociar sin modificar inventario',exact:true}).click();await page.waitForLoadState('domcontentloaded');
  check(await page.getByRole('link',{name:'Consultar tipo global',exact:true}).count()===1,'Misma localidad asociada al segundo evento');
  await page.getByRole('link',{name:'Consultar tipo global',exact:true}).click();
  await page.getByLabel('Precio COP',{exact:true}).fill('11000');await page.getByRole('button',{name:'Guardar localidad',exact:true}).click();
  check(await page.getByRole('alert').filter({hasText:'Localidad compartida'}).count()===1,'Cambio de precio compartido rechazado');
  for(const width of [360,768,1440])await capture('localidad-bloqueada',width);
  await page.reload();check(Number(await page.getByLabel('Precio COP',{exact:true}).inputValue())===10000,'Precio compartido original conservado');
  await page.getByLabel('Capacidad global por evento',{exact:true}).fill('11');await page.getByRole('button',{name:'Guardar localidad',exact:true}).click();check(await page.getByRole('alert').filter({hasText:'Localidad compartida'}).count()===1,'Cambio de capacidad compartida rechazado');
  await page.reload();check(await page.getByLabel('Capacidad global por evento',{exact:true}).inputValue()==='10','Capacidad compartida conservada');
  await page.getByLabel('Nombre global',{exact:true}).focus();await page.keyboard.press('Tab');check(await page.getByLabel('Precio COP',{exact:true}).evaluate(el=>el===document.activeElement),'Tab pasa de nombre a precio');
  await page.goto('https://localhost:7197/administracion');await page.getByRole('button',{name:'Cerrar sesión',exact:true}).click();
  const denied=await page.goto('https://localhost:7197/administracion/eventos');check(denied.status()===401,'Logout protege listado con HTTP 401');
 }catch(e){report.errorType=e.name;report.failure=String(e.message).slice(0,300);console.log('PRUEBA DETENIDA: '+e.name);process.exitCode=2;}
 finally{fs.writeFileSync(path.join(root,process.argv.includes('--finish')?'finish-results.json':'results.json'),JSON.stringify(report,null,2));await browser.close();}
})();
