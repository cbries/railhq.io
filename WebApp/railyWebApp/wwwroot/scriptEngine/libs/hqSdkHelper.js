
async function waitUntilDirectionIs(driverName, address, expectedDirection) {
    const maxRetries = 10;
    const delayMs = 100;

    for (let i = 0; i < maxRetries; i++) {
        const response = await fetch(`${apiBaseLocomotive}/${driverName}/${address}/status`);
        const data = await response.json();
        const intState = expectedDirection === true ? 1 : 0;
        if (data.direction === intState) return;
        await new Promise(resolve => setTimeout(resolve, delayMs));
    }

    throw new Error(`Richtung wurde für ${driverName} ${address} nicht übernommen`);
}
