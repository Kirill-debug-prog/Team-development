function showHidePassword(target){
	var input = target.closest('.password-wrapper').querySelector('.form-input');
	if (input.getAttribute('type') == 'password') {
		input.setAttribute('type', 'text');
        target.classList.add('to-hide');
	} else {
		input.setAttribute('type', 'password');
        target.classList.remove('to-hide');
	}
}

const loginInput = document.querySelector('#login');
const passwordInput = document.querySelector('#password');
const passwordRepeatedInput = document.querySelector('#password_repeated');

const passwordInputs = document.querySelectorAll('#password, #password_repeated');
passwordInputs.forEach((input) => {
	addEventListener('input', () => {
		eyeElement = input.parentElement.querySelector('.show-hide-password');
		if (input.value.length > 0) {
			eyeElement.classList.remove('display-none');
		} else {
			eyeElement.classList.add('display-none');
		}
	})
})



class FormsValidation {
	selectors = {
		form: '[data-js-form]',
		fieldErrors: '[data-js-form-field-errors]'
	}
	
	errorMessages = {
		valueMissing: () => 'Поле обязательно для заполнения',
		patternMismatch: () => 'Данные не соответствуют фомату',
		tooShort: ({ minLength }) => `Минимальное количество символов - ${minLength}`,
		tooLong: ({ maxLength }) => `Максимальное количество символов - ${maxLength}`
	}

	constructor() {
		this.bindEvents();
	}

	manageErrors(fieldControlElement, errorMessages) {
		const fieldErrorsElement = fieldControlElement.parentElement.querySelector(this.selectors.fieldErrors);

		fieldErrorsElement.innerHTML = errorMessages
		.map((message) => `<span class="field-error">${message}</span>`)
		.join('');
	}

	validateField(fieldControlElement) {
		const errors = fieldControlElement.validity;
		const errorMessages = [];

		Object.entries(this.errorMessages).forEach(([errorType, getErrorMessage]) => {
			if (errors[errorType]) {
				errorMessages.push(getErrorMessage(fieldControlElement));
			}
		})

		if (fieldControlElement.matches('#password_repeated') && passwordRepeatedInput.value.length>0 && passwordInput.value.length>0 && passwordRepeatedInput.value!==passwordInput.value) {
			errorMessages.push('Пароли не совпадают');
		}

		this.manageErrors(fieldControlElement, errorMessages);

		return errorMessages.length === 0;
	}

	onInput(event) {
		const { target } = event;
		const isFormField = target.closest(this.selectors.form);
		const isRequired = target.required;

		if (isFormField && isRequired) {
			this.validateField(target);
		}

		if (event.target.matches('#password') && passwordRepeatedInput.value.length>0 && passwordInput.value.length>0) {
			this.validateField(passwordRepeatedInput);
		}
	}

	onSubmit(event) {
		const isFormElement = event.target.matches(this.selectors.form);
		
		if (!isFormElement) {
			return;
		}

		const requiredControlElements = [...event.target.elements].filter(({ required }) => required);
		let isFormValid = true;
		let firstInvalidFieldControl = null;

		requiredControlElements.forEach((element) => {
			const isFieldValid = this.validateField(element);

			if (!isFieldValid) {
				isFormValid = false;
					
				if (!firstInvalidFieldControl) {
					firstInvalidFieldControl = element;
				}
			}
		});

		event.preventDefault();

		if (!isFormValid) {
			firstInvalidFieldControl.focus();
		} else {
			const phoneRegex = /^((8|\+7|7)[\- ]?9\d{2}[\- ]?\d{3}[\- ]?\d{2}[\- ]?\d{2}[ ]?)$/;
			if (phoneRegex.test(loginInput.value)) {
				const digitsOnly = loginInput.value.replace(/[^0-9]/g, '');
				loginInput.value = digitsOnly;
			}

			makeRegisterRequest();
		}
	}

	bindEvents() {
		const requiredControlElements = [...document.querySelector(this.selectors.form).elements].filter(({ required }) => required);
		requiredControlElements.forEach((inputElement) => {
			inputElement.onblur = (event) => {
				this.onInput(event);
				inputElement.addEventListener('input', (event) => this.onInput(event));
				inputElement.onblur = null;
			}
		})

		document.addEventListener('submit', (event) => this.onSubmit(event));
	}
}

new FormsValidation();

function makeRegisterRequest() {
	const formElement = document.querySelector('[data-js-form]');
	const formData = new FormData(formElement);
	const formDataObject = Object.fromEntries(formData);

	fetch('http://89.169.3.43/api/auth/register', {
		method: 'post',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({...formDataObject})
	})
	.then(async (response) => {

		if (!response.ok) {
			if (response.status !== 400) {
				throw new Error('Что-то пошло не так, попробуйте еще раз');
			}
		}

		if (!response.ok) {
			if (response.status !== 400) {
				throw new Error('Что-то пошло не так, попробуйте еще раз');
			}
			const json = await response.json();
			throw new Error(json.message === "Username already exists" ? 'Данный логин уже занят' : 'Логин или пароль не соответствуют формату');
		}
		return response.json();
	})
	.then((json) => {
		makeLoginRequest(json.user.login, json.user.password);
	})
	.catch((error) => {
		const formErrorsElement = document.querySelector("[data-js-form-errors]");
		formErrorsElement.innerHTML = `${error.message}`;
	})
}

function makeLoginRequest(login, password) {
	const formDataObject = { 
		"login": login,
		"password": password
	}

	fetch('http://89.169.3.43/api/auth/login', {
		method: 'post',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({...formDataObject})
	})
	.then(async (response) => {
		if (!response.ok) {
			throw new Error('Регистрация прошла успешно, но произошла ошибка при входе. Попробуйте войти самостоятельно');
		}
		return response.json();
	})
	.then((json) => {
		Object.entries(json).forEach(([name, value]) => {
			document.cookie = `${encodeURIComponent(name)}=${encodeURIComponent(value)}`;
		});

		window.location.href = './mentors_cards_list.html';
	})
	.catch((error) => {
		const formErrorsElement = document.querySelector("[data-js-form-errors]");
		formErrorsElement.innerHTML = `${error.message}`;
	})
}