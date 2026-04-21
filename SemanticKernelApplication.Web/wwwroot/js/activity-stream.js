export function startActivityStream(dotNetRef, url) {
    const eventSource = new EventSource(url);

    const handler = (event) => {
        const payload = JSON.parse(event.data);
        dotNetRef.invokeMethodAsync("OnActivityEvent", payload);
    };

    eventSource.addEventListener("activity", handler);

    return {
        stop: () => {
            eventSource.removeEventListener("activity", handler);
            eventSource.close();
        }
    };
}
