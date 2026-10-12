document.addEventListener("DOMContentLoaded", function () {
    // 1. Sidebar Toggle
    const sidebarToggleBtn = document.getElementById("sidebarToggleBtn");
    const sidebar = document.getElementById("crm-sidebar");

    if (sidebarToggleBtn && sidebar) {
        sidebarToggleBtn.addEventListener("click", function () {
            if (window.innerWidth < 992) {
                sidebar.classList.toggle("show");
            } else {
                sidebar.classList.toggle("collapsed");
            }
        });
    }

    // Close mobile sidebar when clicking outside
    document.addEventListener("click", function (e) {
        if (window.innerWidth < 992 && sidebar && sidebar.classList.contains("show")) {
            if (!sidebar.contains(e.target) && !sidebarToggleBtn.contains(e.target)) {
                sidebar.classList.remove("show");
            }
        }
    });

    // 2. Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll(".alert-dismissible");
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });
});

// Confirmation Dialog Helper
function confirmAction(message, formId) {
    if (confirm(message || "Are you sure you want to perform this action?")) {
        const form = document.getElementById(formId);
        if (form) form.submit();
    }
    return false;
}
