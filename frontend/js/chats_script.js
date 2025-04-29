const chatItemsElements = document.querySelectorAll(".chat-item");
const currentChatElement = document.querySelector(".current-chat");
chatItemsElements.forEach(chat => {
    chat.addEventListener("click", () => {
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
                    <div class="current-interlocutor-name">Фамилия Имя</div>
                    <div class="current-chat-title">Краткое описание (title)</div>
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

        chatItemsElements.forEach( (chatItem) => {chatItem.classList.remove("picked-chat")});
        chat.classList.add("picked-chat");


        const closeChatButton = document.querySelector(".close-chat-button");
        closeChatButton.addEventListener("click", () => {
            currentChatElement.innerHTML = `<p class="pick-chat-text">Выберите чат...</p>`;
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


        currentChatElement.classList.add('mobile-display-block');
        const chatsElement = document.querySelector(".chats");
        chatsElement.classList.add('mobile-display-none');

        const mobileCloseButtonElement = document.querySelector(".mobile-close-chat-button");
        mobileCloseButtonElement.addEventListener('click', () => {
            chatsElement.classList.remove('mobile-display-none');
            currentChatElement.classList.remove('mobile-display-block');
        });
    })
})


