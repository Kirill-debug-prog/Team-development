// загрузка инфы о профиле
document.addEventListener("DOMContentLoaded", async() => {
    try {
        setUserName();
        await loadUserCards();
    } catch (error) {
        document.body.innerHTML = `Ошибка "${error.message}". Попробуйте перезагрузить страницу`;
    }
    
 });

function setUserName() {
    const lastNameElements = document.querySelectorAll('span.last-name');
    lastNameElements.forEach((lastNameElement) => {
        lastNameElement.innerHTML = `${localStorage.getItem('lastName')}`;
    });

    const firstNameElements = document.querySelectorAll('span.first-name');
    firstNameElements.forEach((firstNameElement) => {
        firstNameElement.innerHTML = `${localStorage.getItem('firstName')}`;
    });

    document.querySelector('span.patronymic').innerHTML = `${localStorage.getItem('patronymic')}`;
}

function redirectToLogin() {
    localStorage.setItem('expiredMessage', 'Ваша сессия устарела.');

    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    window.location.href = './login.html';
}

function getCookie(name) {
  for (const entryStr of document.cookie.split('; ')) {
    const [entryName, entryValue] = entryStr.split('=');

    if (decodeURIComponent(entryName) === name) {
        return entryValue;
    }
  }
}

async function loadUserCards() {
    try {
        const token = getCookie('token');
        if (!token) {
            redirectToLogin();
        }

        const response = await fetch('http://89.169.3.43/api/users/me/mentor-cards', {
            method: 'get',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.status === 401) {
            redirectToLogin();
            return;
        }

        if (!response.ok) {
            throw new Error(`${response.status}`);
        }

        const userCardsData = await response.json();
        addCards(userCardsData);
        
    } catch (error) {
        throw error;
    }
}

function addCards(userCardsData) {
    const cardList = document.querySelector('.card-list');
    if (userCardsData.length === 0) {
        cardList.innerHTML = `<div class="text-centered">У вас нет анкет</div>`;
    } else {
        cardList.innerHTML = ``;

        userCardsData.forEach(({id, title, experiences, description, pricePerHours} = cardInfo) => {
            let experienceDuration = 0;
            experiences.forEach((experience) => {
                experienceDuration += experience.durationYears;
            });
            
            let card = document.createElement("article");
            card.classList.add("mentor-card");
            card.setAttribute('data-card-id',`${id}`);
            card.innerHTML = `
                <div class="card-owner">
                    <img class="card-user-avatar" 
                        src="../images/default_user_photo.jpg" 
                        alt="Аватар консультанта"
                        width="76" height="76"
                    />
                    <div class="card-user-name">${localStorage.getItem('lastName')} ${localStorage.getItem('firstName')}</div>
                </div>

                <h3 class="card-title">
                    ${title}
                </h3>

                <div class="card-experience">Опыт работы: <span class="work-experience">${experienceDuration} лет</span></div>

                <div class="card-short-description">
                    ${description}
                </div>
                
                <div class="card-footer">
                    <div class="card-price">${pricePerHours} руб.</div>
                    <a class="show-full-card-button" href="./mentor_card.html?id=${id}">
                        Подробнее...
                    </a>
                </div>

                <div class="card-actions-wrapper">
                    <a href="./modify_card.html?id=${id}" class="transparent-button">Редактировать</a>
                    <button class="transparent-button delete-card-button" type="button">Удалить</button>
                </div>                                          
            `;

            cardList.appendChild(card);
        });
    }
}



// окно редактирования ФИО и авы
const ImageBeforeElement = document.querySelector('.avatar-before');
const avatarActionsElement = document.querySelector('.avatar-actions');
ImageBeforeElement.addEventListener('click', (event) => {
    avatarActionsElement.classList.toggle('display-none');
})

modifyingUserInfo.addEventListener('click', (event) => {
    if (!event.target.closest('.avatar-before')) {
        avatarActionsElement.classList.add('display-none');
    }
})

function modifyUserWindow() {
    modifyingUserInfo.showModal();
    modifyingUserInfo.querySelector('input#last-name').value = document.querySelector('span.last-name').innerHTML;
    modifyingUserInfo.querySelector('input#first-name').value = document.querySelector('span.first-name').innerHTML;
    modifyingUserInfo.querySelector('input#patronymic').value = document.querySelector('span.patronymic').innerHTML;
    modifyingUserInfo.querySelector('input#last-name').blur(); // убираем фокус с 1го поля

    document.querySelector('.dialog-user-avatar').setAttribute("src", "../images/default_user_photo.jpg");
    formsValidation.bindEvents(modifyingUserInfo);
}

function saveNewUserInfo(button) {
    if (formsValidation.isFormValid()) {
        changeUserInfoRequest(button);
    }
}

function changeUserInfoRequest(button) {
    const formElement = document.querySelector('[data-modify-user-info-form]');
	const formData = new FormData(formElement);
	const formDataObject = Object.fromEntries(formData);
    const token = getCookie('token');

	fetch('http://89.169.3.43/api/users/me', {
		method: 'put',
		headers: {
			'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
		},
		body: JSON.stringify({...formDataObject})
	})
	.then(async (response) => {
		if (response.status === 401) {
            redirectToLogin();
            return;

        } else if (!response.ok) {
            const json = await response.json();
            throw new Error();
        
        } else {
            closeWindow(button);
            showingSuccess.showModal();

            localStorage.setItem('firstName',`${formDataObject.firstName}`);
		    localStorage.setItem('lastName',`${formDataObject.lastName}`);
		    localStorage.setItem('patronymic',`${formDataObject.middleName}`);
            setUserName();
        }
	})
	.catch((error) => {
		const formErrorsElement = formElement.querySelector("[data-js-form-errors]");
		formErrorsElement.innerHTML = 'Ошибка. Повторите попытку';
	})
}

const avatarInputElement = document.querySelector('#avatar');
avatarInputElement.addEventListener('change', (event) => {
  let file = avatarInputElement.files[0];
  let reader = new FileReader();

  reader.onload = function() {
    const img = new Image();
    img.src = reader.result;

    img.onload = () => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');

      const smallestSide = Math.min(img.width, img.height);

      canvas.width = smallestSide;
      canvas.height = smallestSide;

      const xOffset = (img.width - smallestSide) / 2;
      const yOffset = (img.height - smallestSide) / 2;

      ctx.drawImage(img, xOffset, yOffset, smallestSide, smallestSide, 0, 0, smallestSide, smallestSide);

      const croppedDataUrl = canvas.toDataURL('image/jpeg');
      document.querySelector('.dialog-user-avatar').setAttribute("src", croppedDataUrl);
    };
  };

  reader.readAsDataURL(file);

});



// класс валидации для форм в обоих окнах
class FormsValidation {
	currentWindow;
	
	errorMessages = {
		valueMissing: () => 'Поле обязательно для заполнения',
        patternMismatch: () => 'Данные не соответствуют фомату',
		tooShort: ({ minLength }) => `Минимальное количество символов - ${minLength}`,
		tooLong: ({ maxLength }) => `Максимальное количество символов - ${maxLength}`
	}

	manageErrors(fieldControlElement, errorMessages) {
		const fieldErrorsElement = fieldControlElement.parentElement.querySelector('[data-js-form-field-errors]');

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

        if (fieldControlElement.matches('#newPassword') && newPasswordRepeated.value.length>0 && newPassword.value.length>0 && newPasswordRepeated.value!==newPassword.value) {
            this.manageErrors(newPasswordRepeated, ['Пароли не совпадают']);
        }

		if (fieldControlElement.matches('#newPasswordRepeated') && newPasswordRepeated.value.length>0 && newPassword.value.length>0 && newPasswordRepeated.value!==newPassword.value) {
			errorMessages.push('Пароли не совпадают');
		}

		this.manageErrors(fieldControlElement, errorMessages);

		return errorMessages.length === 0;
	}

	onInput(event) {
		const { target } = event;
		this.validateField(target);
	}

	isFormValid() {
        const inputElements = this.currentWindow.querySelectorAll(".dialog-input");
		let isFormValid = true;
		let firstInvalidFieldControl = null;

		inputElements.forEach((element) => {
			const isFieldValid = this.validateField(element);

			if (!isFieldValid) {
				isFormValid = false;
					
				if (!firstInvalidFieldControl) {
					firstInvalidFieldControl = element;
				}
			}
		});

		if (!isFormValid) {
			firstInvalidFieldControl.focus();
		}

        return isFormValid;
	}

	bindEvents(window) {
        this.currentWindow = window;
		const inputElements = window.querySelectorAll(".dialog-input");
		inputElements.forEach((inputElement) => {
			inputElement.onblur = (event) => {
				this.onInput(event);
				inputElement.oninput = (event) => { this.onInput(event) }
				inputElement.onblur = null;
			}
		});
	}

    unbindEvents(window) {
        this.currentWindow = null;
        const inputElements = window.querySelectorAll(".dialog-input");
        inputElements.forEach((inputElement) => {
			inputElement.oninput = null;
		});
    }
}
const formsValidation = new FormsValidation();



// изменение пароля
function modifyPasswordWindow() {
    modifyingPassword.showModal();
    formsValidation.bindEvents(modifyingPassword);
}

function saveNewPassword(button) {
    if (formsValidation.isFormValid()) {
        changePasswordRequest(button);
    }
}

function showHidePassword(target){
	var input = target.closest('.password-input-wrapper').querySelector('.dialog-input');
	if (input.getAttribute('type') == 'password') {
		input.setAttribute('type', 'text');
        target.classList.add('to-hide');
	} else {
		input.setAttribute('type', 'password');
        target.classList.remove('to-hide');
	}
}

function changePasswordRequest(button) {
	const formElement = document.querySelector('[data-modify-password-form]');
	const formData = new FormData(formElement);
	const formDataObject = Object.fromEntries(formData);
    const token = getCookie('token');

	fetch('http://89.169.3.43/api/users/me/change-password', {
		method: 'post',
		headers: {
			'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
		},
		body: JSON.stringify({...formDataObject})
	})
	.then(async (response) => {
		if (response.status === 401) {
            redirectToLogin();
            return;

        } else if (!response.ok) {
            const json = await response.json();
            throw new Error(`${json.message}`);
        
        } else {
            closeWindow(button);
            showingSuccess.showModal();
        }
	})
	.catch((error) => {
		const formErrorsElement = formElement.querySelector("[data-js-form-errors]");
		formErrorsElement.innerHTML = `${error.message}`;
	})
}



// закрытие, для всех окон
function closeWindow(closeButton) {
    let dialogWindow = closeButton.closest('dialog');
    if (dialogWindow) {
        dialogWindow.close();
    }

    if (dialogWindow.matches('#modifyingPassword')) {
        inputs = dialogWindow.querySelectorAll('input');
        inputs.forEach((input) => {
            input.value = '';
            input.setAttribute('type', 'password');
        })

        eyes = dialogWindow.querySelectorAll('.show-hide-password');
        eyes.forEach((eye) => {
            eye.classList.remove('to-hide');
        })

        errorMessages = dialogWindow.querySelectorAll('.field-errors, .form-errors');
        errorMessages.forEach((errorMessage) => {
            errorMessage.innerHTML = '';
        })

        formsValidation.unbindEvents(modifyingPassword);
    }

    if (dialogWindow.matches('#modifyingUserInfo')) {
        errorMessages = dialogWindow.querySelectorAll('.field-errors, .form-errors');
        errorMessages.forEach((errorMessage) => {
            errorMessage.innerHTML = '';
        })

        formsValidation.unbindEvents(modifyingUserInfo);
    }
}



// удаление, окно подтверждения удаления
let cardToDelete;
function removeCard(button) {
    const cardId = cardToDelete.getAttribute('data-card-id');
    const token = getCookie('token');
    
	fetch(`http://89.169.3.43/api/consultant-cards/${cardId}`, {
		method: 'delete',
		headers: {
			'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
		}
	})
	.then((response) => {
		if (response.status === 401) {
            redirectToLogin();
            return;
        } else if (!response.ok) {
            closeWindow(button);
            throw new Error();
        } else {
            closeWindow(button);
            if (cardToDelete) {
                cardToDelete.remove();
                if (!cardListElement.querySelector('.mentor-card'))
                {
                    cardList.innerHTML = `<div class="text-centered">У вас нет анкет</div>`;
                }
            }
        }
	})
	.catch(() => {
		showingError.showModal();
	})
}

const cardListElement = document.querySelector('.card-list');
cardListElement.addEventListener('click', (event) => {
    if (event.target.matches('.delete-card-button')) {
        cardToDelete = event.target.closest('.mentor-card');
        deletingСonfirmation.showModal();
    }
})