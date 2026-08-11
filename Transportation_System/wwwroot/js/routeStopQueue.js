document.addEventListener("DOMContentLoaded", function(event) {
    updateButtonStates();
})
document.addEventListener('click', async (e) => {

    const btn = e.target.closest('.move-up, .move-down, .remove-stop');
    if (!btn) return;

    const li = btn.closest('li[data-stop-id]');
    if (!li) return;

    const queue = document.getElementById('stopQueue');
    if (!queue) return;

    const routeId = queue.dataset.routeId;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    if (btn.classList.contains('move-up')) {
        const prev = li.previousElementSibling;
        
        if (prev && prev.dataset.stopId) {
            queue.insertBefore(li, prev);
            updateButtonStates();
            syncInputs();
        }
    }

    else if (btn.classList.contains('move-down')) {
        const next = li.nextElementSibling;
        
        if (next && next.dataset.stopId) {
            queue.insertBefore(next, li);
            updateButtonStates();
            syncInputs();
        }
    }

    else if (btn.classList.contains('remove-stop')) {
            const stopId = li.dataset.stopId;
            if (!confirm('Remove this stop from the route?')) return;
            
            deleteStop(stopId);
            li.remove();
            updateButtonStates();
            syncInputs();
    }
});

function updateButtonStates() {
    const queue = document.getElementById('stopQueue');
    if (!queue) return;
    
    const items = queue.querySelectorAll('li[data-stop-id]');
    
    items.forEach((li, index) => {
        const upBtn = li.querySelector('.move-up');
        const downBtn = li.querySelector('.move-down');
        if (upBtn) upBtn.disabled = index === 0;
        if (downBtn) downBtn.disabled = index === items.length - 1;
    });

    let emptyMsg = document.getElementById('emptyStopMessage');
    if (items.length === 0 && !emptyMsg) {
        emptyMsg = document.createElement('li');
        emptyMsg.className = 'list-group-item text-muted';
        emptyMsg.textContent = 'No stops on this route yet.';
        emptyMsg.id = 'emptyStopMessage';
        queue.appendChild(emptyMsg);
    } else if (items.length > 0 && emptyMsg) {
        emptyMsg.remove();
    }
}

function syncInputs () {
    const queue = document.getElementById('stopQueue');
    const container = document.getElementById('routeStopsInputs');
    
    if (!queue || !container) return;
    
    container.innerHTML = '';
    queue.querySelectorAll('li[data-stop-id]').forEach((li, index) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'RouteStops';
        input.value = li.dataset.stopId;
        container.appendChild(input);
    })
}

function deleteStop (stopId) {
    const container = document.getElementById('deletedStop');
    if (!container) return;
    
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = 'deletedStopId';
    input.value = stopId;
    container.appendChild(input);
}
