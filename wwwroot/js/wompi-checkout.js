const openWompi = document.getElementById('open-wompi');
const wompiStatus = document.getElementById('wompi-status');

openWompi.addEventListener('click', () => {
    if (typeof window.WidgetCheckout !== 'function') {
        wompiStatus.textContent = 'El widget de Wompi aún no está disponible. Reintenta o abre el Web Checkout.';
        return;
    }

    wompiStatus.textContent = '';
    try {
        const options = JSON.parse(document.getElementById('wompi-options').textContent);
        const checkout = new window.WidgetCheckout(options);
        checkout.open(result => {
            const transactionId = result?.transaction?.id;
            if (!transactionId) {
                wompiStatus.textContent = 'El checkout se cerró. Puedes reintentar con esta misma orden.';
                return;
            }

            const returnUrl = new URL(openWompi.dataset.returnUrl);
            returnUrl.searchParams.set('id', transactionId);
            window.location.assign(returnUrl);
        });
    } catch {
        wompiStatus.textContent = 'No se pudo abrir el widget. Reintenta o usa el Web Checkout.';
    }
});
