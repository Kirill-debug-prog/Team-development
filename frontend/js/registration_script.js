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

		if (isFormField) {
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

		const controlElements = event.target.querySelectorAll('input');
		let isFormValid = true;
		let firstInvalidFieldControl = null;

		controlElements.forEach((element) => {
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
		const controlElements = document.querySelector(this.selectors.form).querySelectorAll('input');
		controlElements.forEach((inputElement) => {
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
			const json = await response.json();
			throw new Error(json.message === "Username already exists" ? 'Данный логин уже занят' : 'Логин или пароль не соответствуют формату');
		}
		return response.json();
	})
	.then(async ({tokenResponse}) => {
		document.cookie = `token=${encodeURIComponent(tokenResponse.token)};expires=${(new Date(tokenResponse.expires)).toUTCString()}`;
		await saveUserDataToLocalStorage();
		window.location.href = './mentors_cards_list.html';
	})
	.catch((error) => {
		const formErrorsElement = document.querySelector("[data-js-form-errors]");
		formErrorsElement.innerHTML = `${error.message}`;
	})
}


async function saveUserDataToLocalStorage() {
	try {
        const token = getCookie('token');

        const response = await fetch('http://89.169.3.43/api/users/me', {
            method: 'get',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error(`${response.status}`);
        }

        const userData = await response.json();

		localStorage.setItem('firstName',`${userData.firstName}`);
		localStorage.setItem('lastName',`${userData.lastName}`);
		localStorage.setItem('patronymic',`${userData.middleName}`);
		localStorage.setItem('id',`${userData.id}`);

    } catch (error) {
        throw error;
    }
}

function getCookie(name) {
  for (const entryStr of document.cookie.split('; ')) {
    const [entryName, entryValue] = entryStr.split('=');

    if (decodeURIComponent(entryName) === name) {
        return entryValue;
    }
  }
}