"use strict";

// Declare signalR variable
const signalR = window.signalR;

// Initialize SignalR connection
let connection = null;

// Start the connection
async function startConnection() {
    try {
        // Check if connection exists and is not in Disconnected state
        if (connection) {
            // Only try to start if it's in the Disconnected state
            if (connection.state === signalR.HubConnectionState.Disconnected) {
                await connection.start();
                console.log("SignalR Reconnected.");
                // Load initial notifications after reconnection
                loadInitialNotifications();
            } else {
                console.log("Connection is already in state:", connection.state);
                return; // Exit if connection is already starting or connected
            }
        } else {
            // Create a new connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect()
                .build();

            // Set up event handlers
            setupSignalREventHandlers();

            // Start the new connection
            await connection.start();
            console.log("SignalR Connected.");

            // Load initial notifications after connection is established
            loadInitialNotifications();
        }
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        // Reset connection to null if it failed to start
        connection = null;
        setTimeout(startConnection, 5000); // Try to reconnect after 5 seconds
    }
}

// Set up SignalR event handlers
function setupSignalREventHandlers() {
    // Handle connection closed
    connection.onclose(async () => {
        console.log("SignalR connection closed. Attempting to reconnect...");
        // Don't call startConnection directly here, as it might cause an infinite loop
        setTimeout(startConnection, 5000);
    });

    // Handle receiving a new notification
    connection.on("ReceiveNotification", function (notification) {
        console.log("Received notification:", notification);
        // Add notification to the list
        addNotificationToList(notification);
    });

    // Handle receiving unread count
    connection.on("ReceiveUnreadCount", function (count) {
        console.log("Received unread count:", count);
        updateNotificationBadgeCount(count);
    });
}

// Add a notification to the notification list
function addNotificationToList(notification) {
    // Only add notifications that are not read
    if (notification.isRead) {
        return; // Skip read notifications
    }

    const notificationList = document.getElementById("notification-list");

    // Check if notificationList exists
    if (!notificationList) {
        console.error("Notification list element not found");
        return;
    }

    // Remove "No new notifications" message if it exists
    const emptyNotification = notificationList.querySelector(".empty-notification");
    if (emptyNotification) {
        notificationList.removeChild(emptyNotification);
    }

    // Create notification element
    const li = document.createElement("li");
    li.className = "py-2 border-b hover:bg-gray-50";
    li.dataset.id = notification.id;
    li.dataset.isRead = "false"; // Track read status in the DOM

    // Format the time
    const timeAgo = getTimeAgo(new Date(notification.createdAt));

    // Set the HTML content
    li.innerHTML = `
        <div class="flex items-start">
            <div class="w-2 h-2 mt-2 rounded-full bg-blue-500 mr-2"></div>
            <div class="flex-1">
                <p class="font-medium text-sm">${notification.title}</p>
                <p class="text-sm text-gray-700">${notification.message}</p>
                <div class="flex justify-between items-center mt-1">
                    <p class="text-xs text-gray-500">${timeAgo}</p>
                    <button class="mark-as-read-btn text-xs text-blue-600 hover:text-blue-800">Mark as Read</button>
                </div>
            </div>
        </div>
    `;

    // Add click event to the "Mark as Read" button
    const markAsReadBtn = li.querySelector(".mark-as-read-btn");
    markAsReadBtn.addEventListener("click", function (event) {
        event.stopPropagation(); // Prevent the li click event from firing
        markNotificationAsRead(notification.id);
        li.dataset.isRead = "true"; // Update read status in the DOM
        li.remove(); // Remove this notification from the list

        // If no notifications left, show the empty message
        if (notificationList.querySelectorAll("li:not(.empty-notification)").length === 0) {
            notificationList.innerHTML = '<li class="text-sm text-gray-700 py-2 border-b empty-notification">No new notifications</li>';
        }

        // Update the badge
        updateNotificationBadge();
    });

    // Add click event to the notification for navigation
    li.addEventListener("click", function () {
        // If it's a property notification with a related property ID, navigate to that property
        if (notification.relatedPropertyId) {
            window.location.href = `/Admin/ViewPropertyDetails/${notification.relatedPropertyId}`;
        }
    });

    // Add to the beginning of the list
    notificationList.insertBefore(li, notificationList.firstChild);

    // Update the badge
    updateNotificationBadge();
}

// Mark a notification as read
function markNotificationAsRead(notificationId) {
    // Make an HTTP request to ensure the change is persisted in the database
    fetch(`/Notification/MarkAsRead/${notificationId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    })
        .then(response => {
            if (response.ok) {
                console.log(`Notification ${notificationId} marked as read`);
                // Still use SignalR to update other connected clients
                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("MarkAsRead", notificationId).catch(function (err) {
                        console.error("SignalR error:", err);
                    });
                }
            } else {
                console.error("Failed to mark notification as read");
            }
        })
        .catch(error => {
            console.error("Error marking notification as read:", error);
        });
}

// Mark all notifications as read and clear them from the UI
function markAllNotificationsAsRead() {
    // Make an HTTP request to ensure the change is persisted in the database
    fetch('/Notification/MarkAllAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    })
        .then(response => {
            if (response.ok) {
                console.log("All notifications marked as read");
                // Still use SignalR to update other connected clients
                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("MarkAllAsRead").catch(function (err) {
                        console.error("SignalR error:", err);
                    });
                }

                // Update UI
                const notificationList = document.getElementById("notification-list");
                if (notificationList) {
                    notificationList.innerHTML = '<li class="text-sm text-gray-700 py-2 border-b empty-notification">No new notifications</li>';
                    updateNotificationBadgeCount(0);
                }
            } else {
                console.error("Failed to mark all notifications as read");
            }
        })
        .catch(error => {
            console.error("Error marking all notifications as read:", error);
        });
}

// Mark all notifications as read without clearing them from the UI
function markAllNotificationsAsReadOnly() {
    // Make an HTTP request to ensure the change is persisted in the database
    fetch('/Notification/MarkAllAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    })
        .then(response => {
            if (response.ok) {
                console.log("All notifications marked as read");

                // Still use SignalR to update other connected clients
                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("MarkAllAsRead").catch(function (err) {
                        console.error("SignalR error:", err);
                    });
                }

                // Update UI - mark all as read but don't clear them
                const notificationList = document.getElementById("notification-list");
                if (!notificationList) {
                    console.error("Notification list element not found");
                    return;
                }

                const notificationItems = notificationList.querySelectorAll("li:not(.empty-notification)");

                if (notificationItems.length === 0) {
                    return; // No notifications to mark as read
                }

                notificationItems.forEach(item => {
                    // Update data attribute
                    item.dataset.isRead = "true";

                    // Remove the blue dot indicator
                    const blueDot = item.querySelector(".w-2.h-2.mt-2.rounded-full.bg-blue-500");
                    if (blueDot) {
                        blueDot.remove();
                    }

                    // Remove the "Mark as Read" button for each notification
                    const markAsReadBtn = item.querySelector(".mark-as-read-btn");
                    if (markAsReadBtn) {
                        markAsReadBtn.remove();
                    }
                });

                // Update the badge count to zero
                updateNotificationBadgeCount(0);
            } else {
                console.error("Failed to mark all notifications as read");
            }
        })
        .catch(error => {
            console.error("Error marking all notifications as read:", error);
        });
}

// Update the notification badge count
function updateNotificationBadgeCount(count) {
    const badge = document.getElementById("notification-badge");
    if (!badge) {
        console.error("Notification badge element not found");
        return;
    }

    if (count > 0) {
        badge.textContent = count > 99 ? "99+" : count;
        badge.classList.remove("hidden");
    } else {
        badge.classList.add("hidden");
    }
}

// Update notification badge based on unread notifications
function updateNotificationBadge() {
    const notificationList = document.getElementById("notification-list");
    if (!notificationList) {
        console.error("Notification list element not found");
        return;
    }

    // Count only notifications that are not marked as read
    const unreadCount = notificationList.querySelectorAll("li:not(.empty-notification)[data-is-read='false']").length;
    updateNotificationBadgeCount(unreadCount);
}

// Helper function to format time ago
function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);

    let interval = Math.floor(seconds / 31536000);
    if (interval >= 1) {
        return interval + " year" + (interval === 1 ? "" : "s") + " ago";
    }

    interval = Math.floor(seconds / 2592000);
    if (interval >= 1) {
        return interval + " month" + (interval === 1 ? "" : "s") + " ago";
    }

    interval = Math.floor(seconds / 86400);
    if (interval >= 1) {
        return interval + " day" + (interval === 1 ? "" : "s") + " ago";
    }

    interval = Math.floor(seconds / 3600);
    if (interval >= 1) {
        return interval + " hour" + (interval === 1 ? "" : "s") + " ago";
    }

    interval = Math.floor(seconds / 60);
    if (interval >= 1) {
        return interval + " minute" + (interval === 1 ? "" : "s") + " ago";
    }

    return "just now";
}

// Load initial notifications from the server
function loadInitialNotifications() {
    fetch("/Notification/GetRecentNotifications")
        .then(response => response.json())
        .then(data => {
            console.log("Loaded initial notifications:", data);
            const notificationList = document.getElementById("notification-list");

            if (!notificationList) {
                console.error("Notification list element not found");
                return;
            }

            // Clear the list
            notificationList.innerHTML = "";

            // Filter to only show unread notifications
            const unreadNotifications = data.filter(notification => !notification.isRead);

            if (unreadNotifications.length === 0) {
                notificationList.innerHTML = '<li class="text-sm text-gray-700 py-2 border-b empty-notification">No new notifications</li>';
                updateNotificationBadgeCount(0);
                return;
            }

            // Add each unread notification
            unreadNotifications.forEach(notification => {
                addNotificationToList(notification);
            });

            // Update badge with count of unread notifications
            updateNotificationBadgeCount(unreadNotifications.length);
        })
        .catch(error => {
            console.error("Error loading notifications:", error);
        });
}

// Toggle notification panel visibility
function toggleNotificationPanel() {
    const panel = document.getElementById("notification-panel");
    if (!panel) {
        console.error("Notification panel element not found");
        return;
    }

    panel.classList.toggle("hidden");
}

// Start the connection when the document is ready
document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM loaded, starting SignalR connection");

    // Only start the connection once when the page loads
    startConnection();

    // Set up event listeners
    const notificationIcon = document.getElementById("notification-icon");
    if (notificationIcon) {
        notificationIcon.addEventListener("click", toggleNotificationPanel);
    }

    const clearBtn = document.getElementById("clear-notifications");
    if (clearBtn) {
        clearBtn.addEventListener("click", markAllNotificationsAsRead);
    }

    // Close notification panel when clicking outside
    document.addEventListener("click", function (event) {
        const panel = document.getElementById("notification-panel");
        const icon = document.getElementById("notification-icon");

        if (panel && !panel.contains(event.target) && event.target !== icon) {
            panel.classList.add("hidden");
        }
    });
});

// Export functions for use in other scripts
window.markNotificationAsRead = markNotificationAsRead;
window.markAllNotificationsAsRead = markAllNotificationsAsRead;
window.markAllNotificationsAsReadOnly = markAllNotificationsAsReadOnly;
window.toggleNotificationPanel = toggleNotificationPanel;