// Comportamiento del lado cliente sin scripts en línea (compatible con la CSP).
(function () {
    "use strict";

    // Validación no intrusiva basada en las anotaciones de los ViewModels (data-val-*).
    if (window.aspnetValidation) {
        new window.aspnetValidation.ValidationService().bootstrap();
    }

    // Confirmación para formularios marcados con data-confirm (p. ej. borrar).
    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.getAttribute("data-confirm"))) {
                event.preventDefault();
            }
        });
    });
})();
