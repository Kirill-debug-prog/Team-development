//проверка на авторизацию
if (getCookie('token')) {
    setUserName();
} else {
    // если токен истек (данные в localStorage остаются)
    if (localStorage.getItem('id')) {
        redirectToLogin();
    }
}

function setUserName() {
    headerElements = document.querySelectorAll('.authorized-menu, .unauthorized-menu, .header-user');
    headerElements.forEach((headerElement) => {
        headerElement.classList.toggle('display-none');
    })

    document.querySelector('.header-logo').classList.remove('unauth-mobile-show');

    document.querySelector('span.last-name').innerHTML = `${localStorage.getItem('lastName')}`;
    document.querySelector('span.first-name').innerHTML = `${localStorage.getItem('firstName')}`;
}

function logout() {
    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    document.cookie = `token=${getCookie('token')};max-age=-1`;

    window.location.reload();
}

function getCookie(name) {
  for (const entryStr of document.cookie.split('; ')) {
    const [entryName, entryValue] = entryStr.split('=');

    if (decodeURIComponent(entryName) === name) {
        return entryValue;
    }
  }
}


document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search)
    const id = urlParams.get('id')

    if (!id) {
        console.error("No ID provided in the URL")
        return
    }

    fetchMentorData(id)
})

async function fetchMentorData(id) {
    try {
        const response = await fetch(`http://89.169.3.43/api/consultant-cards/${id}`)
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)

        const mentor = await response.json()
        renderMentorCard(mentor)
    } catch (error) {
        console.error("Error fetching mentor data:", error)
    }
}

function renderMentorCard(mentor) {
    document.querySelector('.mentor-name').textContent = mentor.mentorFullName || 'Имя не указано'
    document.querySelector('.price-accent').textContent = mentor.pricePerHours + ' ₽'
    document.querySelector('.card-title').textContent = mentor.title || ''
    document.querySelector('.card-description').textContent = mentor.description || ''

    // если авторизованный сейчас пользователь !== создатель анкеты, то показывает кнопку "написать"
    if (localStorage.getItem('id')!==mentor.mentorId) {
        document.querySelector('.message-button').classList.remove('display-none')
    }

    const experienceList = document.querySelector('.experience-list')
    experienceList.innerHTML = ''
    let totalYears = 0

    if (mentor.experiences && Array.isArray(mentor.experiences)) {
        mentor.experiences.forEach(exp => {
            const li = document.createElement('li')
            li.className = 'experience-item'
            li.innerHTML = `
              <div class="experience-position-place">${exp.position}, ${exp.companyName}</div>
              <div class="experience-time">${pluralizeYears(exp.durationYears)}</div>
            `
            totalYears += exp.durationYears || 0
            experienceList.appendChild(li)
        })
    }

    document.querySelector('.experience-total b').textContent = totalYears

    const activityAreasList = document.querySelector('.activity-areas-list')
    activityAreasList.innerHTML = ''

    if (mentor.categories && Array.isArray(mentor.categories) && mentor.categories.length > 0) {
        mentor.categories.forEach(category => {
            const li = document.createElement('li')
            li.className ='activity-area'
            li.textContent = category.name
            activityAreasList.appendChild(li)
        })
    } else {
        const li = document.createElement('li')
        li.textContent = 'Сферы деятельности не указаны'
        activityAreasList.appendChild(li)
    }
}

function pluralizeYears (n) {
    const lastDigit = n % 10
    const lastTwoDigits = n % 100

    if (lastTwoDigits >= 11 && lastTwoDigits <= 14) return `${n} лет`
    if (lastDigit === 1) return `${n} год`
    if (lastDigit >= 2 && lastDigit <= 4) return `${n} года`
    return `${n} лет`
}

async function redirectToChat(cardId) {
    const token = getCookie('token');
    if (!token) {
        if (localStorage.getItem('id')) {
            redirectToLogin();
            return;
        }
        showingUnauth.showModal();
        return;
    }

    const button = document.querySelector('.message-button');
    const mentorId = button?.dataset?.mentorId;

    if (!mentorId) {
        console.error("Ошибка: не удалось получить ID ментора.");
        return;
    }

    try {
        const response = await fetch(`http://89.169.3.43/api/chat/rooms/with/${mentorId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                redirectToLogin();
                return;
            }
            const error = await response.json();
            alert(error?.message || 'Произошла ошибка при создании чата.');
            return;
        }

        const chatRoom = await response.json();
        if (!chatRoom?.id || !mentorId) {
            alert('Ошибка: не удалось получить ID комнаты или ментора.');
            return;
        }

        //Извлекаем title карточки
        const cardResponce = await fetch(`http://89.169.3.43/api/consultant-cards/${cardId}`)
        const cardDate = await cardResponce.json();
        const cardTitle = cardDate.title
        const encodedTitle = decodeURIComponent(cardTitle)
        console.log('Card Title:', encodedTitle)

        localStorage.setItem(`chatTitle-${chatRoom.id}`, cardTitle)

        //Перенаправляем на страницу чата с roomId, mentorId и title
        const roomId = encodeURIComponent(chatRoom.id);
        const mentor = encodeURIComponent(mentorId);
        window.location.href = `./chats.html?roomId=${roomId}&mentorId=${mentor}&title=${encodedTitle}`;

    } catch (error) {
        console.error("Ошибка при создании чата:", error);
        alert('Произошла ошибка при создании чата. Пожалуйста, попробуйте позже.');
    }
}
    

function redirectToLogin() {
    localStorage.setItem('expiredMessage', 'Ваша сессия устарела.');

    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    window.location.href = './login.html';
}

const urlParams = new URLSearchParams(window.location.search)
const cardId = urlParams.get('id')

fetch(`http://89.169.3.43/api/consultant-cards/${cardId}`)
    .then(response => response.json())
    .then(cardData => {
        // Получаем данные из ответа API
        const mentorId = cardData.mentorId
        
        console.log(mentorId)

        document.querySelector('.message-button').dataset.mentorId = mentorId;
    })
