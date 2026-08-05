const map = L.map('map').setView([55.7558, 37.6173], 13);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
}).addTo(map);

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
    loadMapData();
});

window.updateBusMarker = updateBusMarker;
window.addStopMarker = addStopMarker;
window.drawRoute = drawRoute;
window.loadMapData = loadMapData;
window.updateBusRealtime = updateBusRealtime;