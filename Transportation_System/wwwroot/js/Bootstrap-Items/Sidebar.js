(function () {
    window.renderSidebar = function (containerSelector) {
        const container = document.querySelector(containerSelector);
        if (!container) return;

        const currentPath = window.location.pathname;

        function isActive(path) {
            const current = currentPath.toLowerCase().replace(/\/$/, '');
            const target = path.toLowerCase().replace(/\/$/, '');
            
            const match = current === target ||
                current + '/index' === target ||
                target + '/index' === current;

            return match ? 'active' : 'text-white';
        }

        container.innerHTML = `
            <div class="d-flex flex-column flex-shrink-0 p-3 text-bg-dark" 
                 style="width: 280px; height: 100vh; position: sticky; top: 0;">
                <a href="/" class="d-flex align-items-center mb-3 mb-md-0 me-md-auto text-white text-decoration-none">
                    <i class="bi bi-bus-front-fill fs-4 me-2"></i>
                    <span class="fs-4">BusTransport</span>
                </a>
                <hr>
                <ul class="nav nav-pills flex-column mb-auto">
                    <li class="nav-item">
                        <a href="/Home/Dashboard" class="nav-link ${isActive('/Home/Dashboard')}">
                            <i class="bi bi-speedometer2 me-2"></i>
                            Dashboard
                        </a>
                    </li>
                    <li>
                        <a href="/Bus/Index" class="nav-link ${isActive('/Bus/Index')}">
                            <i class="bi bi-bus-front me-2"></i>
                            Buses
                        </a>
                    </li>
                    <li>
                        <a href="/Stop/Index" class="nav-link ${isActive('/Stop/Index')}">
                            <i class="bi bi-sign-stop me-2"></i>
                            Stops
                        </a>
                    </li>
                    <li>
                        <a href="/Route/Index" class="nav-link ${isActive('/Route/Index')}">
                            <i class="bi bi-map me-2"></i>
                            Routes
                        </a>
                    </li>
                </ul>
                <hr>
            </div>
        `;
    };
})();