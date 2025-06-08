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
    await loadChats()
    await startSignalR()
    await joinRoomSignalIR()
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

            let localTitle = localStorage.getItem(`chatTitle-${id}`)
            let finalTitle = localTitle || title

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
                    <div class="chat-title">${finalTitle}</div>
                </div>
                <div class="unread-messages-counter">${realUnreadMessagesCount}</div>                                          
            `;

            chatListElement.appendChild(chat);
        });
    }
}

async function joinRoomSignalIR() {
    const chatItems = document.querySelectorAll('.chat-item');
    for (const chatItem of chatItems) {
        const roomId = chatItem.getAttribute('data-chat-id');
        try {
            await connection.invoke("JoinRoom", roomId);
            console.log(`Успешно присоединились к комнате ${roomId}`);
        } catch (error) {
            console.error(`Ошибка при присоединении к комнате ${roomId}:`, error);
        }
    }
}




// --------------------- ОТРИСОВКА ЧАТА ---------------------
const chatListElement = document.querySelector(".chat-list");
const currentChatElement = document.querySelector(".current-chat");

chatListElement.addEventListener('click', async (event) => {
    const chat = event.target.closest('.chat-item');
    if (!chat) return;

    const counter = chat.querySelector('.unread-messages-counter');
    if (counter) {
        counter.textContent = '';
    }

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

    sendButtonElement.addEventListener("click", async () => {
        const messageText = textareaElement.value.trim();
        if (!messageText) return;

        const roomId = chat.getAttribute('data-chat-id');

        try {

            await sendMessageHttp(roomId, messageText);

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

    currentChatElement.setAttribute('data-chat-id', roomId)
    
    if (connection && connection.state === "Connected") {
        try {
            await connection.invoke("JoinRoom", roomId)
            console.log(`Успешное примоедеение к комнате${roomId}`)
        } catch{
            console.error(`Ошибка присоеденения к комнате ${roomId}:`, error)
        }
    }

    loadMessages(roomId)
});

// --------------------- SIGNALR ---------------------
let connection;

async function startSignalR() {
    if (connection && connection.state === "Connected") {
        await connection.stop();
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`http://89.169.3.43/chathub`, {
            accessTokenFactory: () => getCookie('token'),
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveMessage", (message) => {
        console.log("Новое сообщение:", message);
        handleIncomingMessage(message);
    });


    try {
        await connection.start();
        console.log("Подключение к SignalR успешно установлено");
    } catch (error) {
        console.error("Ошибка подключения к SignalR:", error);
        setTimeout(startSignalR, 5000);
    }

}

let lastRenderedDateGroup = null

function handleIncomingMessage(message) {
    const roomId = message.roomId;
    const pickedChat = document.querySelector('.chat-item.picked-chat')
    const pickedChatRoomId = pickedChat?.getAttribute('data-chat-id')

    console.log('Входящее сообщение:', message)
    console.log('Выбранный id чат-комнаты:', pickedChatRoomId)

    if (pickedChatRoomId === message.roomId) {
        const currentDateGroup = formatDateGroup(message.dateSent)
        renderSingleMessage(message, lastRenderedDateGroup)
        lastRenderedDateGroup = currentDateGroup
        return;
    }

    const chatCard = document.querySelector(`.chat-item[data-chat-id="${roomId}"]`);
    if (!chatCard) return;

    const counter = chatCard.querySelector('.unread-messages-counter');
    let currentCount = parseInt(counter.textContent) || 0;
    counter.textContent = currentCount + 1;
}

function appendMessageToChat(senderId, messageText, dateSent, isRead = false) {

    const messageList = document.querySelector('.message-list')

    const messageDate = dateSent
        ? new Date(dateSent).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
        : new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    let newMessage = document.createElement("div")
    newMessage.classList.add("message");

    const isOwnMessage = senderId === localStorage.getItem('id');
    if (isOwnMessage) {
        newMessage.classList.add("sender-me");
        if (!isRead) {
            newMessage.classList.add("unread-message");
        }
    }

    newMessage.innerHTML = `
        <div class="message-text">${escapeHtml(messageText)}</div>
        <span class="message-time">${messageDate}</span>
    `;

    messageList.appendChild(newMessage);
    messageList.scrollTop = messageList.scrollHeight;
}

// --------------------- ЭКРАНИРОВАНИЕ ---------------------
function escapeHtml(text) {
    const div = document.createElement("div")
    div.innerText = text
    return div.innerHTML
}

// --------------------- ОТКРЫТИЕ ЧАТА ---------------------
async function loadMessages(roomId) {
    try {

        const token = getCookie('token');
        if (!token) {
            redirectToLogin();
            return;
    }

    const respons = await fetch(`http://89.169.3.43/api/chat/rooms/${roomId}/messages`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    })

    if (respons.status === 401) {
        redirectToLogin()
        return;
    }

    if (!respons.ok) {
        throw new Error(`Ошибка: ${respons.status}`)
    }

    const message = await respons.json()
    renderMessages(message)
    console.log(message);
    } catch (error) {
        console.error("Ошибка при загрузке сообщений:", error)
    }
}

// --------------------- ОТРИСОВКА СООБЩЕНИЙ ---------------------
function renderMessages(messages) {
    const messageList = document.querySelector('.message-list');
    messageList.innerHTML = ''
    lastRenderedDateGroup = null

    if (!messages || messages.length == 0) {
        const emptyMessage = document.createElement('p')
        emptyMessage.classList.add('no-messages')
        emptyMessage.textContent = 'Нет сообщений в этом чате'
        messageList.appendChild(emptyMessage)
        return
    }

    let lastDateGroup = null;

    messages.forEach(({ senderId, messageContent, dateSent, isRead }) => {
        const currentDateGroup = formatDateGroup(dateSent)

        // Если дата группы изменилась — вставляем заголовок
        if (currentDateGroup !== lastDateGroup) {
            const dateGroupDiv = document.createElement('div')
            dateGroupDiv.classList.add('messages-date')
            dateGroupDiv.textContent = currentDateGroup
            messageList.appendChild(dateGroupDiv)

            lastDateGroup = currentDateGroup;
        }

        const messageDate = new Date(dateSent).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
        let messageDiv = document.createElement('div')
        messageDiv.classList.add('message')

        const isOwnMessage = senderId === localStorage.getItem('id');
        if (isOwnMessage) {
            messageDiv.classList.add('sender-me');
            if (!isRead) {
                messageDiv.classList.add('unread-message');
            }
        }
        
        messageDiv.innerHTML = `
            <div class="message-text">${escapeHtml(messageContent)}</div>
            <span class="message-time">${messageDate}</span>
        `;

        messageList.appendChild(messageDiv)
    })

    messageList.scrollTop = messageList.scrollHeight
}

// --------------------- ОТРИСОВКА ОДНОГО СООБЩЕНИЯ ---------------------
function renderSingleMessage(message, previousDateGroup  = null) {
    const messageList = document.querySelector('.message-list')
    const currentDateGroup = formatDateGroup(message.dateSent)

    if (currentDateGroup !== previousDateGroup) {
        const dateGroupDiv = document.createElement('div')
        dateGroupDiv.classList.add('messages-date')
        dateGroupDiv.textContent = currentDateGroup
        messageList.appendChild(dateGroupDiv)
    }

    const messageDate = new Date(message.dateSent).toLocaleDateString([], {hour: '2-digit', minute: '2-digit'} )
    let messageDiv  = document.createElement('div')
    messageDiv.classList.add('message')

    const isOwnMessage = message.senderId === localStorage.getItem('id')
    if (isOwnMessage) {
        messageDiv.classList.add('sender-me')
        if(!message.isRead){
            messageDiv.classList.add('unread-message')
        }
    }

    messageDiv.innerHTML = `
        <div class="message-text">${escapeHtml(message.messageContent)}</div>
        <span class="message-time>${messageDate}</span>
    `

    messageList.appendChild(messageDiv)
    messageList.scrollTop = messageList.scrollHeight
}



// --------------------- оТПРАВКА СООБЩЕНИЙ ---------------------
async function sendMessageHttp(roomId, messageText) {
    const token = getCookie('token');

    if (!messageText.trim()) return;

    const body = {
        messageContent: messageText.trim()
    };

    try {
        console.log("Отправляем сообщение на сервер", roomId, messageText);
        const response = await fetch(`http://89.169.3.43/api/chat/rooms/${roomId}/messages`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(body)
        });

        if (response.status === 201) {
            const messageDto = await response.json();
            console.log("Сообщение успешно отправлено", messageDto);
            renderSingleMessage(messageDto)
            return messageDto;
        } else if (response.status === 401) {
            redirectToLogin();
        } else {
            const errorData = await response.json();
            console.error("Ошибка при отправке сообщения:", errorData)
        }
    } catch (error) {
        console.error("Ошибка сети при отправке сообщения:", error)
    }
}

function formatDateGroup(dateString) {
    const date = new Date(dateString)
    const now = new Date()

    const dateDay = new Date(date.getFullYear(), date.getMonth(), date.getDate())
    const nowDay = new Date(now.getFullYear(), now.getMonth(), now.getDate())

    const diffTime = nowDay - dateDay
    const diffDays = diffTime / (1000 * 60 * 60 * 24)

    if (diffDays === 0) return 'Сегодня';
    if (diffDays === 1) return 'Вчера';

    return date.toLocaleDateString('ru-RU', {day: 'numeric', month: 'long'});
}