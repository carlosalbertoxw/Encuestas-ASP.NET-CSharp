function radio_group_checked(nombre) {
    for (var i = 0; i < document.getElementsByName(nombre).length; i++) {
        if (document.getElementsByName(nombre)[i].checked) {
            return true;
        }
    }
    return false;
}

function add_poll_answer() {
    if (!radio_group_checked('stars')) {
        alert('Seleccione un valor del campo Estrellas');
        return false;
    } else {
        return true;
    }
}

function val_email(field) {
    if (!/^[^@\s]+@[^@\.\s]+(\.[^@\.\s]+)+$/.test(field.value.trim())) {
        return false;
    } else {
        return true;
    }
}

function sign_in(form) {
    if (form.email.value.trim().length === 0) {
        alert('El correo electrónico y contraseña son obligatorios');
        form.email.focus();
        return false;
    } else
    if (!val_email(form.email)) {
        alert('Ingrese un correo electrónico válido');
        form.email.focus();
        return false;
    } else
    if (form.password.value.trim().length === 0) {
        alert('El correo electrónico y contraseña son obligatorios');
        form.password.focus();
        return false;
    } else
    if (form.password.value.trim().length < 6) {
        alert('La contraseña es muy corta');
        form.password.focus();
        return false;
    } else {
        return true;
    }
}

function sign_up(form) {
    if (form.email.value.trim().length === 0) {
        alert('El correo electrónico es obligatorio');
        form.email.focus();
        return false;
    } else
    if (!val_email(form.email)) {
        alert('Ingrese un correo electrónico válido');
        form.email.focus();
        return false;
    } else
    if (form.password.value.trim().length === 0) {
        alert('La contraseña es obligatoria');
        form.password.focus();
        return false;
    } else
    if (form.password.value.trim().length < 6) {
        alert('La contraseña no puede ser menor a seis caracteres');
        form.password.focus();
        return false;
    } else
    if (form.password.value.trim() !== form.rePassword.value.trim()) {
        alert('Las contraseñas no coinciden');
        return false;
    } else {
        return true;
    }
}
