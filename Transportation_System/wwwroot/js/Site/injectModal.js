function openModal(item, id, target) {
    fetch('/'+ item + '/'+ target + '/' + id)
        .then(r => r.text())
        .then(html => {
            document.getElementById(target + 'ModalBody').innerHTML = html;
            new bootstrap.Modal(document.getElementById(target + 'Modal')).show();
        });
}

document.addEventListener('submit', function(e) {
    const form = e.target;
    if (!form.matches('.ajax-form')) return;

    e.preventDefault();
    if (window.jQuery && $(form).data('validator') && !$(form).valid()) return;

    fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(response => {
            if (response.redirected) {
                window.location.href = response.url;
                return null;
            }
            return response.text();
        })
        .then(html => {
            if (!html) return;
            const modalBody = form.closest('.modal-body');
            if (modalBody) modalBody.innerHTML = html;

            const newForm = modalBody?.querySelector('form');
            if (newForm && window.jQuery) {
                $(newForm).removeData("validator").removeData("unobtrusiveValidation");
                $.validator.unobtrusive.parse(newForm);
            }
        });
});