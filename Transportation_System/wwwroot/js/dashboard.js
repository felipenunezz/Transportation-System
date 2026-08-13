const connection = new signalR.HubConnectionBuilder()
    .withUrl("/busHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.start().then(() => {
    console.log("Connected to SignalR hub");
}).catch(err => console.error(err));


connection.on("BusUpdated", async (busId) => {
    console.log("Bus Id received:", busId);
    var bus = await fetchBus(busId);
    if (!bus) return;
    
    updateBusMarker(bus);

    const busCard = document.querySelector(`.bus-card[data-bus-id="${busId}"]`);
    if (busCard) {
        const speedElement = busCard.querySelector('.speed');
        if (speedElement) speedElement.textContent = bus.speed;

        const passengerElement = busCard.querySelector('.passengerCount');
        if (passengerElement) passengerElement.textContent = bus.passengerCount;

        const latElement = busCard.querySelector('.latitude');
        if (latElement) latElement.textContent = bus.currentLatitude;

        const lngElement = busCard.querySelector('.longitude');
        if (lngElement) lngElement.textContent = bus.currentLongitude;
    }
});

document.addEventListener('DOMContentLoaded', () => {
    const stops = window.__dashboardData?.busStops || [];
    stops.forEach(stop => {
        addStopMarker(stop.id, stop.name, stop.latitude, stop.longitude);
    });
})
async function fetchBus (id) {
    try {
        const bus = await connection.invoke("GetBusAsync", id);
        console.log("Bus returned from server:", bus);
        return bus;
    } catch (err) {
        console.error("Failed to fetch bus", id, err);
        return null;
    }
}