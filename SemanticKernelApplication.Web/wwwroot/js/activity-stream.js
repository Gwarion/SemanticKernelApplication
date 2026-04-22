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

const conversationKey = "agent-workbench-active-conversation";

export function getActiveConversationId() {
    return sessionStorage.getItem(conversationKey);
}

export function setActiveConversationId(conversationId) {
    if (!conversationId) {
        sessionStorage.removeItem(conversationKey);
        return;
    }

    sessionStorage.setItem(conversationKey, conversationId);
}
