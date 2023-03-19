function poll(form) {
    if (form.title.value.trim().length === 0) {
        alert('El título es obligatorio');
        form.title.focus();
        return false;
    } else
    if (form.position.value.trim().length === 0) {
        alert('La posición es obligatoria');
        form.position.focus();
        return false;
    } else
    if (isNaN(parseInt(form.position.value))) {
        alert('La posición solo acepta valores númericos enteros');
        form.position.focus();
        return false;
    } else
    if (parseInt(form.position.value) <= 0) {
        alert('La posición solo acepta valores mayores a 0');
        form.position.focus();
        return false;
    } else {
        return true;
    }
}

function delete_account(form) {
    if (form.password.value.trim().length === 0) {
        alert('La contraseña es obligatoria');
        form.password.focus();
        return false;
    } else
    if (form.password.value.trim().length < 6) {
        alert('La contraseña no puede ser menor a seis caracteres');
        form.password.focus();
        return false;
    } else {
        return true;
    }
}

function val_uri(field) {
    var exp = /^([0-9a-zA-Z-])*$/;
    if (exp.test(field.value.trim())) {
        return true;
    }
    return false;
}

function val_email(field) {
    if (!/^[^@\s]+@[^@\.\s]+(\.[^@\.\s]+)+$/.test(field.value.trim())) {
        return false;
    } else {
        return true;
    }
}

function change_password(form) {
    if (form.newPassword.value.trim().length === 0) {
        alert('La nueva contraseña es obligatoria');
        form.newPassword.focus();
        return false;
    } else
    if (form.newPassword.value.trim().length < 6) {
        alert('La nueva contraseña no puede ser menor a seis caracteres');
        form.newPassword.focus();
        return false;
    } else
    if (form.newPassword.value.trim() !== form.reNewPassword.value.trim()) {
        alert('Las contraseñas no coinciden');
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
    } else {
        return true;
    }
}

function change_email(form) {
    if (form.email.value.trim().length === 0) {
        alert('El correo electrónico es obligatorio');
        form.email.focus();
        return false;
    } else
    if (!val_email(form.email)) {
        alert('Ingresa un correo electrónico válido');
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
    } else {
        return true;
    }
}

function change_user(form) {
    if (form.userName.value.trim().length === 0) {
        alert('El usuario es obligatorio');
        form.userName.focus();
        return false;
    } else
    if (!val_uri(form.userName)) {
        alert('Solo se aceptan los caracteres 0-9 A-Z a-z - en el usuario');
        form.userName.focus();
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
    } else {
        return true;
    }
}

function edit_profile(form) {
    if (form.name.value.trim().length === 0) {
        alert('El nombre es obligatorio');
        form.name.focus();
        return false;
    } else {
        return true;
    }
}
