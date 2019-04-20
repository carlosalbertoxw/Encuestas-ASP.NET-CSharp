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
            if (!val_numeric(form.position)) {
                alert('La posición solo acepta valores númericos enteros');
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
    if (form.new_password.value.trim().length === 0) {
        alert('La nueva contraseña es obligatoria');
        form.new_password.focus();
        return false;
    } else
        if (form.new_password.value.trim().length < 6) {
            alert('La nueva contraseña no puede ser menor a seis caracteres');
            form.new_password.focus();
            return false;
        } else
            if (form.new_password.value.trim() !== form.re_new_password.value.trim()) {
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
    if (form.user.value.trim().length === 0) {
        alert('El usuario es obligatorio');
        form.user.focus();
        return false;
    } else
        if (!val_uri(form.user)) {
            alert('Solo se aceptan los caracteres 0-9 A-Z a-z - en el usuario');
            form.user.focus();
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
