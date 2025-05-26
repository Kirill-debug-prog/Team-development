//проверка на авторизацию
if (getCookie('token')) {
    setUserName();
} else {
    window.location.href = './login.html';
}

function setUserName() {
    document.querySelector('span.last-name').innerHTML = `${localStorage.getItem('lastName')}`;
    document.querySelector('span.first-name').innerHTML = `${localStorage.getItem('firstName')}`;
}


// для выхода по нажатию иконки
function logout() {
    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    document.cookie = `token=${getCookie('token')};max-age=-1`;

    window.location.href = './mentors_cards_list.html';
}


// если токен истек (очищение localStorage и установка свойства expiredMessage, для показа уведомления 'Ваша сессия устарела.' на странице входа)
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


document.addEventListener("DOMContentLoaded", async() => {
    await loadChats();
});

async function loadChats() {
    try {
        const token = getCookie('token');
        if (!token) {
            redirectToLogin();
        }

        const response = await fetch('http://89.169.3.43/api/chat/rooms', {
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

        const chats = await response.json();
        console.log(chats)
        addChats(chats);
        
    } catch (error) {
        document.body.innerHTML = `Ошибка "${error.message}". Попробуйте перезагрузить страницу`;
    }
}

function addChats(chats) {
    const chatListElement = document.querySelector('.chat-list');
    if (chats.length === 0) {
        document.querySelector('.chats-none').innerHTML = `У вас нет чатов`;
    } else {
        chatListElement.innerHTML = ``;

        chats.forEach(({id, clientName, mentorName, mentorId, title, lastMessage, unreadMessagesCount} = chatInfo) => {
            let chat = document.createElement("li");
            chat.classList.add("chat-item");
            chat.setAttribute('data-chat-id',`${id}`);

            //определение, кто является собеседником
            isUserMentor = mentorId === localStorage.getItem('id') ? true : false;
            const interlocutorName = isUserMentor ? clientName : mentorName;

            //определение числа непрочитанных сообщений, являются ли они непрочитанными для авторизированного пользователя
            const realUnreadMessagesCount = unreadMessagesCount === 0 ? '' : lastMessage.senderId === localStorage.getItem('id') ? '' : unreadMessagesCount;

            chat.innerHTML = `
                <img src="../images/default_user_photo.jpg"
                    alt="Фотография собеседника"
                    class="interlocutor-avatar"
                    width="40" height="40"
                />
                <div class="chat-info-wrapper">
                    <div class="interlocutor-name">${interlocutorName}</div>
                    <div class="chat-title">${title}</div>
                </div>
                <div class="unread-messages-counter">${realUnreadMessagesCount}</div>                                          
            `;

            chatListElement.appendChild(chat);
        });
    }
}





const chatListElement = document.querySelector(".chat-list");
const currentChatElement = document.querySelector(".current-chat");

chatListElement.addEventListener('click', (event) => {
    if (event.target.closest('.chat-item')) {
        chat = event.target.closest('.chat-item');

        currentChatElement.innerHTML = `
            <header class="chat-header">
                <button class="close-chat-button"></button>
                <button class="mobile-close-chat-button"></button>
                <img src="../images/default_user_photo.jpg"
                        alt="Фотография собеседника"
                        class="current-interlocutor-avatar"
                        width="55" height="55"
                />
                <div class="chat-info-wrapper">
                    <div class="current-interlocutor-name">${chat.querySelector('.interlocutor-name').innerHTML}</div>
                    <div class="current-chat-title">${chat.querySelector('.chat-title').innerHTML}</div>
                </div>
            </header>
            <div class="chat-body">
                <div class="message-list">
                    <div class="messages-date-block">
                        <div class="messages-date">Today</div>
                        <div class="message sender-me">
                            <div class="message-text">Привет! Как дела?</div>
                            <span class="message-time">18:16</span>
                        </div>
    
                        <div class="message">
                            <div class="message-text">Привет! Все отлично, спасибо. У тебя как?</div>
                            <span class="message-time">18:16</span>
                        </div>
    
                        <div class="message sender-me unread-message">
                            <div class="message-text">Хорошо</div>
                            <span class="message-time">18:16</span>
                        </div>
                    </div>
                </div>
                <form class="message-form" action="">
                    <textarea class="message-input" placeholder="Напиши сообщение..." id="message-input" spellcheck></textarea>
                    <button class="send-message-button" type="button">
                        <span class="visually-hidden">Отправить сообщение</span>
                        <img class="send-icon" src="../icons/send_icon.svg" alt="" width="30" height="30" />
                    </button>
                </form>
            </div>`;

        document.querySelectorAll(".chat-item").forEach( (chatItem) => {chatItem.classList.remove("picked-chat")});
        chat.classList.add("picked-chat");


        const closeChatButton = document.querySelector(".close-chat-button");
        closeChatButton.addEventListener("click", () => {
            currentChatElement.innerHTML = `<p class="pick-chat-text">Выберите чат...</p>`;
            document.querySelectorAll(".chat-item").forEach( (chatItem) => {chatItem.classList.remove("picked-chat")});
        })


        const textareaElement = document.getElementById("message-input");
        textareaElement.addEventListener("input", () => {
            textareaElement.style.height = 0;
            textareaElement.style.height = textareaElement.scrollHeight + "px";
        })


        sendButtonElement = document.querySelector(".send-message-button");
        messageListElement = document.querySelector(".message-list");
        messagesDateBlockElement = document.querySelector(".messages-date-block");
        sendButtonElement.addEventListener("click", () => {
            const messageText = textareaElement.value;
            if (!messageText) return;

            textareaElement.style.height = 0;
            textareaElement.value = '';
            const messageDate = (new Date()).getHours() + ":" + (new Date()).getMinutes();

            let newMessage = document.createElement("div");
            newMessage.classList.add("message", "sender-me", "unread-message");
            newMessage.innerHTML = `
                <div class="message-text">${messageText}</div>
                <span class="message-time">${messageDate}</span>                                        
            `;
            messagesDateBlockElement.append(newMessage);

            messageListElement.scrollTo({
                top: messageListElement.scrollHeight
            });
        })


        currentChatElement.classList.add('mobile-display-block'); // на ширине моб. устройств будет видет текущий чат
        const chatsElement = document.querySelector(".chats");
        chatsElement.classList.add('mobile-display-none'); // на ширине моб. устройств не будет видет список чатов

        // при закрытии текущего чата, окно тек. чата скрывается, список чатов показывается
        const mobileCloseButtonElement = document.querySelector(".mobile-close-chat-button");
        mobileCloseButtonElement.addEventListener('click', () => {
            chatsElement.classList.remove('mobile-display-none');
            currentChatElement.classList.remove('mobile-display-block');
        });
    }
})
