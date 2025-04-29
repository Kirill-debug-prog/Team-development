// окно редактирования ФИО и авы
const ImageBeforeElement = document.querySelector('.image-before');
const avatarActionsElement = document.querySelector('.avatar-actions');
ImageBeforeElement.addEventListener('click', (event) => {
    avatarActionsElement.classList.toggle('display-none');
})

modifyingUserInfo.addEventListener('click', (event) => {
    if (!event.target.closest('.image-before')) {
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
}

function saveNewUserInfo(button) {
    closeWindow(button);
    //...
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



// изменение парооля
function saveNewPassword(button) {
    closeWindow(button);
    //...
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



// закрытие, для всех окон
function closeWindow(closeButton) {
    let dialogWindow = closeButton.closest('dialog');
    if (dialogWindow) {
        dialogWindow.close();
    }

    if (dialogWindow.matches('.modify-password-window')) {
        inputs = dialogWindow.querySelectorAll('input');
        inputs.forEach((input) => {
            input.value = '';
            input.setAttribute('type', 'password');
        })

        eyes = dialogWindow.querySelectorAll('.show-hide-password');
        eyes.forEach((eye) => {
            eye.classList.remove('to-hide');
        })
    }


}



// окно подтверждения удаления
let cardToDelete;
function removeCard(button) {
    closeWindow(button);
    if (cardToDelete) {
        cardToDelete.remove();
    }
    
}

const cardListElement = document.querySelector('.card-list');
cardListElement.addEventListener('click', (event) => {
    if (event.target.matches('.delete-card-button')) {
        cardToDelete = event.target.closest('.mentor-card');
        deletingСonfirmation.showModal()
    }

})