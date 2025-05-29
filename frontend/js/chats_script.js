//проверка на авторизацию
if (getCookie('token')) {
    setUserName();
} else {
    // если токен истек (данные в localStorage остаются)
    if (localStorage.getItem('id')) {
        redirectToLogin();
    }

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
            const isUserMentor = mentorId === localStorage.getItem('id') ? true : false;
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




// --------------------- ОТРИСОВКА ЧАТА ---------------------
const chatListElement = document.querySelector(".chat-list");
const currentChatElement = document.querySelector(".current-chat");

chatListElement.addEventListener('click', (event) => {
    const chat = event.target.closest('.chat-item');
    if (!chat) return;

    document.querySelector('.message-list')?.remove();

    currentChatElement.innerHTML = `
        <header class="chat-header">
            <button class="close-chat-button"></button>
            <button class="mobile-close-chat-button"></button>
            <img src="../images/default_user_photo.jpg" alt="Фотография собеседника" class="current-interlocutor-avatar" width="55" height="55" />
            <div class="chat-info-wrapper">
                <div class="current-interlocutor-name">${chat.querySelector('.interlocutor-name').innerHTML}</div>
                <div class="current-chat-title">${chat.querySelector('.chat-title').innerHTML}</div>
            </div>
        </header>
        <div class="chat-body">
            <div class="message-list"></div>
            <form class="message-form">
                <textarea class="message-input" placeholder="Напиши сообщение..." id="message-input" spellcheck></textarea>
                <button class="send-message-button" type="button">
                    <span class="visually-hidden">Отправить сообщение</span>
                    <img class="send-icon" src="../icons/send_icon.svg" alt="" width="30" height="30" />
                </button>
            </form>
        </div>`;

    document.querySelectorAll(".chat-item").forEach(chatItem => chatItem.classList.remove("picked-chat"));
    chat.classList.add("picked-chat");

    const textareaElement = document.getElementById("message-input");
    textareaElement.addEventListener("input", () => {
        textareaElement.style.height = 0;
        textareaElement.style.height = textareaElement.scrollHeight + "px";
    });

    const sendButtonElement = document.querySelector(".send-message-button");
    const messageListElement = document.querySelector(".message-list");

    sendButtonElement.addEventListener("click", async () => {
        const messageText = textareaElement.value.trim();
        if (!messageText) return;

        const userId = localStorage.getItem('id');
        const roomId = chat.getAttribute('data-chat-id');

        try {
            await connection.invoke("SendMessage", roomId, userId, messageText);
            textareaElement.value = '';
            textareaElement.style.height = 0;
        } catch (error) {
            console.error("Ошибка отправки сообщения:", error);
        }
    });

    const closeChatButton = document.querySelector(".close-chat-button");
    closeChatButton.addEventListener("click", () => {
        currentChatElement.innerHTML = `<p class="pick-chat-text">Выберите чат...</p>`;
        document.querySelectorAll(".chat-item").forEach(chatItem => chatItem.classList.remove("picked-chat"));
    });

    const chatsElement = document.querySelector(".chats");
    currentChatElement.classList.add('mobile-display-block');
    chatsElement.classList.add('mobile-display-none');

    const mobileCloseButtonElement = document.querySelector(".mobile-close-chat-button");
    mobileCloseButtonElement.addEventListener('click', () => {
        chatsElement.classList.remove('mobile-display-none');
        currentChatElement.classList.remove('mobile-display-block');
    });

    const roomId = chat.getAttribute('data-chat-id');
    startSignalR(roomId);
});

// --------------------- SIGNALR ---------------------
let connection;

async function startSignalR(roomId) {
    if (connection && connection.state === "Connected") {
        await connection.stop();
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`http://89.169.3.43/chathub?roomId=${roomId}`, {
            accessTokenFactory: () => getCookie('token'),
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveMessage", (senderId, messageText) => {
        console.log("Новое сообщение:", messageText);
        appendMessageToChat(senderId, messageText);
    });

    try {
        await connection.start();
        console.log("Подключение к SignalR успешно установлено");
    } catch (error) {
        console.error("Ошибка подключения к SignalR:", error);
        setTimeout(() => startSignalR(roomId), 5000);
    }
}

function appendMessageToChat(senderId, messageText) {
    const messageList = document.querySelector('.message-list');
    const messageDate = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    let newMessage = document.createElement("div");
    newMessage.classList.add("message");
    if (senderId === localStorage.getItem('id')) {
        newMessage.classList.add("sender-me");
    }

    newMessage.innerHTML = `
        <div class="message-text">${escapeHtml(messageText)}</div>
        <span class="message-time">${messageDate}</span>
    `;

    messageList.appendChild(newMessage);
}

// --------------------- ЭКРАНИРОВАНИЕ ---------------------
function escapeHtml(text) {
    const div = document.createElement("div");
    div.innerText = text;
    return div.innerHTML;
}