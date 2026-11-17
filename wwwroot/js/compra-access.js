export async function activar(compraId, token) {
    const url = new URL(`api/compras/${compraId}/acceso`, document.baseURI);
    const respuesta = await fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(token)
    });

    if (!respuesta.ok) {
        throw new Error('No se pudo activar el acceso a la compra.');
    }
}
