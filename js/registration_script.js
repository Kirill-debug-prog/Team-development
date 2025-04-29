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