// показ сообщения об устаревшей сессии
document.addEventListener("DOMContentLoaded", () => {
    expiredMessage = localStorage.getItem('expiredMessage');
	if (expiredMessage) {
		const expiredMessageElement = document.querySelector(".expiredSessionMessage");
		expiredMessageElement.classList.remove('display-none');
		localStorage.removeItem('expiredMessage');
	}
 });


// скрытие и показ пароля через нажатие на "глаз"
function showHidePassword(target) {
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
const eyeElement = document.querySelector('.show-hide-password');
passwordInput.addEventListener('input', () => {
	if (passwordInput.value.length > 0) {
		eyeElement.classList.remove('display-none');
	} else {
		eyeElement.classList.add('display-none');
	}
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
		})

		event.preventDefault();

		if (!isFormValid) {
			firstInvalidFieldControl.focus();
		} else {
			const phoneRegex = /^((8|\+7|7)[\- ]?9\d{2}[\- ]?\d{3}[\- ]?\d{2}[\- ]?\d{2}[ ]?)$/;
			if (phoneRegex.test(loginInput.value)) {
				const digitsOnly = loginInput.value.replace(/[^0-9]/g, '');
				loginInput.value = digitsOnly;
			}

			makeLoginRequest();
		}
	}

	bindEvents() {
		const handleFirstBlur = (inputElement) => {
			return (event) => {
				this.onInput(event);
				inputElement.addEventListener('input', (event) => this.onInput(event));
				inputElement.onblur = null;
			};
		}

		loginInput.onblur = handleFirstBlur(loginInput);
		passwordInput.onblur = handleFirstBlur(passwordInput);

		document.addEventListener('submit', (event) => this.onSubmit(event));
	}
}

new FormsValidation();

function makeLoginRequest() {
	const formElement = document.querySelector('[data-js-form]');
	const formData = new FormData(formElement);
	const formDataObject = Object.fromEntries(formData);

	fetch('http://89.169.3.43/api/auth/login', {
		method: 'post',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({...formDataObject})
	})
	.then(async (response) => {
		if (!response.ok) {
			const json = await response.json();
			if (json.message === "User doesn't exist" || json.message === "Invalid password") {
				throw new Error('Неверный логин или пароль');
			}
			throw new Error('Что-то пошло не так, попробуйте еще раз');
		}
		return response.json();
	})
	.then(async (json) => {
		document.cookie = `token=${encodeURIComponent(json.token)};expires=${(new Date(json.expires)).toUTCString()}`;
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