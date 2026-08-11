let map;
let selectionMap;
let SelectMarker;

const busMarkers = new Map();
const stopMarkers = new Map();

const busIcon = L.divIcon({
    html: '🚌',
    iconSize: [30, 30],
    className: 'bus-icon'
});

const stopIcon = L.divIcon({
    html: '🚏',
    iconSize: [25, 25],
    className: 'stop-icon'
});

function updateBusMarker(busId, latitude, longitude, busNumber, speed, passengers) {
    const latLng = [latitude, longitude];

    if (busMarkers.has(busId)) {
        busMarkers.get(busId).setLatLng(latLng);
    } else {
        const marker = L.marker(latLng, { icon: busIcon })
            .bindPopup(`<b>${busNumber}</b><br>Speed: ${speed} km/h<br>Passengers: ${passengers}`)
            .addTo(map);
        busMarkers.set(busId, marker);
    }

    busMarkers.get(busId).getPopup()
        .setContent(`<b>${busNumber}</b><br>Speed: ${speed} km/h<br>Passengers: ${passengers}`);
}

function addStopMarker(stopId, name, latitude, longitude, waitingPassengers) {
    const marker = L.marker([latitude, longitude], { icon: stopIcon })
        .bindPopup(`<b>${name}</b><br>Waiting: ${waitingPassengers} passengers`)
        .addTo(map);
    stopMarkers.set(stopId, marker);
}

function drawRoute(routeCoordinates, color = '#3388ff') {
    L.polyline(routeCoordinates, {
        color: color,
        weight: 3,
        opacity: 0.7
    }).addTo(map);
}

async function loadMapData() {
    try {
        const response = await fetch('/api/mapdata');
        const data = await response.json();

        console.log('Loaded data:', data);

        if (data.stops && data.stops.length > 0) {
            data.stops.forEach(stop => {
                addStopMarker(stop.id, stop.name, stop.latitude, stop.longitude, stop.waitingPassengers);
            });
            console.log(`Added ${data.stops.length} stops`);
        }

        if (data.routes && data.routes.length > 0) {
            const colors = ['#3388ff', '#ff6633', '#33cc33', '#ff33cc'];
            data.routes.forEach((route, index) => {
                if (route.stops && route.stops.length > 0) {
                    const coordinates = route.stops.map(stop => [stop.latitude, stop.longitude]);
                    drawRoute(coordinates, colors[index % colors.length]);
                }
            });
            console.log(`Added ${data.routes.length} routes`);
        }

        if (data.buses && data.buses.length > 0) {
            data.buses.forEach(bus => {
                if (bus.currentLatitude && bus.currentLongitude) {
                    updateBusMarker(
                        bus.id,
                        bus.currentLatitude,
                        bus.currentLongitude,
                        bus.busNumber,
                        bus.speed,
                        bus.passengerCount
                    );
                }
            });
            console.log(`Added ${data.buses.length} buses`);
        }

        if (busMarkers.size > 0 || stopMarkers.size > 0) {
            const allMarkers = [...busMarkers.values(), ...stopMarkers.values()];
            const group = L.featureGroup(allMarkers);
            map.fitBounds(group.getBounds().pad(0.1));
        }

    } catch (error) {
        console.error('Error loading map data:', error);
    }
}

function updateBusRealtime(busId, latitude, longitude, speed, passengerCount) {
    const busNumber = `BUS-${busId.toString().padStart(3, '0')}`;
    updateBusMarker(busId, latitude, longitude, busNumber, speed, passengerCount);
}

document.addEventListener('DOMContentLoaded', function() {
    if (!document.getElementById('map')) return; // this page has no dashboard map

    map = L.map('map').setView([56.326797, 44.006516], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    loadMapData();
});

window.updateBusMarker = updateBusMarker;
window.addStopMarker = addStopMarker;
window.drawRoute = drawRoute;
window.loadMapData = loadMapData;
window.updateBusRealtime = updateBusRealtime;
document.addEventListener('shown.bs.modal', function (e) {
    const modal = e.target;
    const mapContainer = modal.querySelector('#selectionMap');
    if (!mapContainer) return;
    
    const latInput = modal.querySelector('#Latitude');
    const lngInput = modal.querySelector('#Longitude');
    const displayLat = modal.querySelector('#displayLat');
    const displayLng = modal.querySelector('#displayLng');

    if (selectionMap) {
        selectionMap.remove();
        selectionMap = null;
        SelectMarker = null;
    }

    selectionMap = L.map(mapContainer).setView([56.326797, 44.006516], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(selectionMap);
    
    if (latInput.value && lngInput.value) {
        const startLatLng = [parseFloat(latInput.value), parseFloat(lngInput.value)];
        SelectMarker = L.marker(startLatLng).addTo(selectionMap);
        selectionMap.setView(startLatLng, 15);
    }

    selectionMap.on('click', function (e) {
        const lat = e.latlng.lat.toFixed(6);
        const lng = e.latlng.lng.toFixed(6);

        latInput.value = lat;
        lngInput.value = lng;
        if (displayLat) displayLat.textContent = lat;
        if (displayLng) displayLng.textContent = lng;

        if (SelectMarker) selectionMap.removeLayer(SelectMarker);
        SelectMarker = L.marker(e.latlng).addTo(selectionMap);
    });

    setTimeout(() => selectionMap.invalidateSize(), 100);
});