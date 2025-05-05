document.getElementById('registerForm').addEventListener('submit', function (event) {
    event.preventDefault(); // Ngăn form submit ngay lập tức

    let isValid = true;
    let errors = {};

    // Lấy giá trị các trường
    const firstName = document.getElementById('reg-fname').value.trim();
    const lastName = document.getElementById('reg-lname').value.trim();
    const birthDate = document.getElementById('birth-date').value; // Lấy giá trị từ trường date
    const gender = document.getElementById('gender').value;
    const email = document.getElementById('reg-email').value.trim();
    const password = document.getElementById('reg-password').value.trim();

    // Reset thông báo lỗi
    document.querySelectorAll('.error-message').forEach(element => element.textContent = '');

    // Kiểm tra trường không được để trống
    if (!firstName) {
        errors['reg-fname'] = 'First Name không được để trống.';
        isValid = false;
    }

    if (!lastName) {
        errors['reg-lname'] = 'Last Name không được để trống.';
        isValid = false;
    }

    if (!email) {
        errors['reg-email'] = 'Email không được để trống.';
        isValid = false;
    }

    if (!password) {
        errors['reg-password'] = 'Password không được để trống.';
        isValid = false;
    }

    if (gender === 'Select') {
        errors['gender'] = 'Vui lòng chọn giới tính.';
        isValid = false;
    }

    // Kiểm tra ngày sinh
    if (!birthDate) {
        errors['birth-date'] = 'Vui lòng chọn ngày sinh.';
        isValid = false;
    } else {
        const birthDateObj = new Date(birthDate);
        const today = new Date();

        // Kiểm tra ngày sinh hợp lệ
        if (isNaN(birthDateObj.getTime())) {
            errors['birth-date'] = 'Ngày sinh không hợp lệ.';
            isValid = false;
        } else if (birthDateObj > today) {
            errors['birth-date'] = 'Ngày sinh không được nằm trong tương lai.';
            isValid = false;
        }
    }

    // Kiểm tra email hợp lệ
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (email && !emailRegex.test(email)) {
        errors['reg-email'] = 'Email không hợp lệ.';
        isValid = false;
    }

    // Kiểm tra mật khẩu (ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt)
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
    if (password && !passwordRegex.test(password)) {
        errors['reg-password'] = 'Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.';
        isValid = false;
    }

    // Hiển thị thông báo lỗi
    Object.keys(errors).forEach(field => {
        const message = errors[field];
        const errorElement = document.getElementById(`${field}-error`);
        if (errorElement) {
            errorElement.textContent = message;
        }
    });

    // Nếu hợp lệ, submit form
    if (isValid) {
        console.log("Form hợp lệ, đang submit...");
        console.log("Giá trị birth-date:", document.getElementById('birth-date').value);
        this.submit();
    } else {
        console.log("Form không hợp lệ:", errors);
    }
});