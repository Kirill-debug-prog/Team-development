// скрытие и показ пароля через нажатие на "глаз"
function showHidePassword(target) {
  var input = target.closest(".password-wrapper").querySelector(".form-input");
  if (input.getAttribute("type") == "password") {
    input.setAttribute("type", "text");
    target.classList.add("to-hide");
  } else {
    input.setAttribute("type", "password");
    target.classList.remove("to-hide");
  }
}

const loginInput = document.querySelector("#login");
const passwordInput = document.querySelector("#password");
const eyeElement = document.querySelector(".show-hide-password");
passwordInput.addEventListener("input", () => {
  if (passwordInput.value.length > 0) {
    eyeElement.classList.remove("display-none");
  } else {
    eyeElement.classList.add("display-none");
  }
});

class FormsValidation {
  selectors = {
    form: "[data-js-form]",
    fieldErrors: "[data-js-form-field-errors]",
  };

  errorMessages = {
    valueMissing: () => "Поле обязательно для заполнения",
    patternMismatch: () => "Данные не соответствуют фомату",
    tooShort: ({ minLength }) =>
      `Минимальное количество символов - ${minLength}`,
    tooLong: ({ maxLength }) =>
      `Максимальное количество символов - ${maxLength}`,
  };

  constructor() {
    this.bindEvents();
  }

  manageErrors(fieldControlElement, errorMessages) {
    const fieldErrorsElement = fieldControlElement.parentElement.querySelector(
      this.selectors.fieldErrors
    );

    fieldErrorsElement.innerHTML = errorMessages
      .map((message) => `<span class="field-error">${message}</span>`)
      .join("");
  }

  validateField(fieldControlElement) {
    const errors = fieldControlElement.validity;
    const errorMessages = [];

    Object.entries(this.errorMessages).forEach(
      ([errorType, getErrorMessage]) => {
        if (errors[errorType]) {
          errorMessages.push(getErrorMessage(fieldControlElement));
        }
      }
    );

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

    const requiredControlElements = [...event.target.elements].filter(
      ({ required }) => required
    );
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

    makeRequest();
  }

  bindEvents() {
    const handleFirstBlur = (inputElement) => {
      return (event) => {
        this.onInput(event);
        inputElement.addEventListener("input", (event) => this.onInput(event));
        inputElement.onblur = null;
      };
    };

    loginInput.onblur = handleFirstBlur(loginInput);
    passwordInput.onblur = handleFirstBlur(passwordInput);

    document.addEventListener("submit", (event) => this.onSubmit(event));
  }
}

new FormsValidation();

function makeRequest() {
  const formElement = document.querySelector("[data-js-form]");
  const formData = new FormData(formElement);
  const formDataObject = Object.fromEntries(formData);

  fetch("http://89.169.3.43/api/auth/login", {
    method: "post",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ ...formDataObject }),
  })
    .then(async (response) => {
      if (!response.ok) {
        if (response.status == 401) {
          throw new Error("Неверный логин или пароль");
        }

        throw new Error("Что-то пошло не так, попробуйте еще раз");
      }
      return response.json();
    })
    .then((json) => {
      Object.entries(json).forEach(([name, value]) => {
        document.cookie = `${encodeURIComponent(name)}=${encodeURIComponent(
          value
        )}`;
      });

      window.location.href = "./mentors_cards_list.html";
    })
    .catch((error) => {
      const formErrorsElement = document.querySelector("[data-js-form-errors]");
      formErrorsElement.innerHTML = `${error.message}`;
    });
}
