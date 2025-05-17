//динамической загрузки карточек с сервера
document.addEventListener("DOMContentLoaded", function() {
    //загрузка ккарточки при загрузки страницы
    loadConsultanstCards()
 });
 
 async function loadConsultanstCards() {
    try {
        const response = await fetch('http://89.169.3.43/api/consultant-cards')

        if (!response.ok) {
            throw new Error('Failed to fetch consultant cards')
        }

        const carts = await response.json()

        const container = document.getElementById('mentor-card-container')
        container.innerHTML = '' // очищаем контейнер перед добавлением новых карточек

        if (carts.length === 0) {
            container.innerHTML = '<p>Карты консультантов не найдены</p>'
        }

        carts.forEach(card => {
            const cartElement = createMentorCard(card)
            container.appendChild(cartElement)
            console.log(card)
        });
    } catch (error) {
        console.error('Error loading consultant cards:', error)
        const container = document.getElementById('mentor-card-container')
        container.innerHTML = '<p>Не удалось загрузить карточки консультантов. Попробуйте еще раз позже.</p>'
    }
}

function createMentorCard (card) {
    const cartArticle = document.createElement('article')
    cartArticle.classList.add('mentor-card')

    cartArticle.innerHTML = `
        <div class="card-owner">
            <img class="card-user-avatar" 
                src="${'../images/default_user_photo.jpg'}" 
                alt="Аватар консультанта"
                width="76" height="76"
            />
            <div class="user-names">${card.mentorFullName}</div>
        </div>

        <h2 class="card-title">${card.title}</h2>

        <div class="card-experience">
            Опыт работы: <span class="work-experience">
        ${formatTotalExperience(card.experiences)}
    </span>
        </div>

        <div class="card-short-description">
        ${getFirstSentences(card.description, 4)}
        </div>
    
        <div class="card-footer">
            <div class="card-price">${card.pricePerHours}</div>
            <a class="show-full-card-button" href="./mentor_card.html?id=${card.id}">
                Подробнее
            </a>
        </div>
    `

    return cartArticle
}

function getFirstSentences(text, count) {
    if (!text) return '';

    const sentences = text.split(/(?<=[.!?])\s+/); 
    return sentences.slice(0, count).join(' ');
}


function formatTotalExperience(experiences) {
    if (!Array.isArray(experiences)) return '0 лет'

    const total = experiences.reduce((sum, exp) => sum + (exp.durationYears || 0), 0)

    const lastDigit = total % 10
    const lastTwoDigits = total % 100

    if (lastTwoDigits >= 11 && lastTwoDigits <= 14) return `${total} лет`
    if (lastDigit === 1) return `${total} год`
    if (lastDigit >= 2 && lastDigit <= 4) return `${total} года`
    return `${total} лет`
}


// Ввод цены
const priceRangeInput = document.querySelectorAll(".price-range"),
priceInput = document.querySelectorAll(".price-input"),
priceProgress = document.querySelector(".price-progress");
let priceGap = 0;

// связь ввода input number с изменением input range
priceInput.forEach(input =>{
    input.addEventListener("input", e =>{
        let minPrice = parseInt(priceInput[0].value),
        maxPrice = parseInt(priceInput[1].value);

        if((maxPrice - minPrice >= priceGap) && (minPrice >= priceRangeInput[0].min) && (maxPrice <= priceRangeInput[1].max)){
            if(e.target.classList.contains("min-price-input")){
                priceRangeInput[0].value = minPrice;
                priceProgress.style.left = ((minPrice / priceRangeInput[0].max) * 100) + "%";
            }else{
                priceRangeInput[1].value = maxPrice;
                priceProgress.style.right = 100 - (maxPrice / priceRangeInput[1].max) * 100 + "%";
            }
        }
    });
});

priceInput.forEach(input =>{
    input.addEventListener("change", e =>{
        let minPrice = parseInt(priceInput[0].value),
        maxPrice = parseInt(priceInput[1].value);
        
        if(!(maxPrice - minPrice >= priceGap)){
            if (e.target.classList.contains("min-price-input")) {
                priceInput[0].value = +priceInput[1].value - priceGap;
            }else{
                priceInput[1].value = +priceInput[0].value + priceGap;
            }
        }

        if(minPrice < priceRangeInput[1].min){
            priceInput[0].value = priceRangeInput[1].min;
        }

        if(maxPrice > priceRangeInput[1].max){
            priceInput[1].value = priceRangeInput[1].max;
        }

        priceRangeInput[0].value = +priceInput[0].value;
        priceProgress.style.left = ((+priceInput[0].value / priceRangeInput[0].max) * 100) + "%";
        priceRangeInput[1].value = +priceInput[1].value;
        priceProgress.style.right = 100 - (+priceInput[1].value / priceRangeInput[1].max) * 100 + "%";
    });
});

// связь ввода input range с изменением input number
priceRangeInput.forEach(input =>{
    input.addEventListener("input", e =>{
        let minVal = parseInt(priceRangeInput[0].value),
        maxVal = parseInt(priceRangeInput[1].value);

        if((maxVal - minVal) < priceGap){
            if(e.target.classList.contains("min-price-range")){
                priceRangeInput[0].value = maxVal - priceGap
            }else{
                priceRangeInput[1].value = minVal + priceGap;
            }
        }else{
            priceInput[0].value = minVal;
            priceInput[1].value = maxVal;
            priceProgress.style.left = ((minVal / priceRangeInput[0].max) * 100) + "%";
            priceProgress.style.right = 100 - (maxVal / priceRangeInput[1].max) * 100 + "%";
        }
    });
});



// Ввод опыта
const experienceRangeInput = document.querySelectorAll(".experience-range"),
experienceInput = document.querySelectorAll(".experience-input"),
experienceProgress = document.querySelector(".experience-progress");
let experienceGap = 0;

// связь ввода input number с изменением input range
experienceInput.forEach(input =>{
    input.addEventListener("input", e =>{
        let minExperience = parseInt(experienceInput[0].value),
        maxExperience = parseInt(experienceInput[1].value);
        
        if((maxExperience - minExperience >= experienceGap) && (minExperience >= experienceRangeInput[0].min) && (maxExperience <= experienceRangeInput[1].max)){
            if(e.target.classList.contains("min-experience-input")){
                experienceRangeInput[0].value = minExperience;
                experienceProgress.style.left = ((minExperience / experienceRangeInput[0].max) * 100) + "%";
            }else{
                experienceRangeInput[1].value = maxExperience;
                experienceProgress.style.right = 100 - (maxExperience / experienceRangeInput[1].max) * 100 + "%";
            }
        }
    });
});

experienceInput.forEach(input =>{
    input.addEventListener("change", e =>{
        let minExperience = parseInt(experienceInput[0].value),
        maxExperience = parseInt(experienceInput[1].value);
        
        if(!(maxExperience - minExperience >= experienceGap)){
            if (e.target.classList.contains("min-experience-input")) {
                experienceInput[0].value = +experienceInput[1].value - experienceGap;
            }else{
                experienceInput[1].value = +experienceInput[0].value + experienceGap;
            }
        }

        if(minExperience < experienceRangeInput[1].min){
            experienceInput[0].value = experienceRangeInput[1].min;
        }

        if(maxExperience > experienceRangeInput[1].max){
            experienceInput[1].value = experienceRangeInput[1].max;
        }
        
        experienceRangeInput[0].value = +experienceInput[0].value;
        experienceProgress.style.left = ((+experienceInput[0].value / experienceRangeInput[0].max) * 100) + "%";
        experienceRangeInput[1].value = +experienceInput[1].value;
        experienceProgress.style.right = 100 - (+experienceInput[1].value / experienceRangeInput[1].max) * 100 + "%";
    });
});

// связь ввода input range с изменением input number
experienceRangeInput.forEach(input =>{
    input.addEventListener("input", e =>{
        let minVal = parseInt(experienceRangeInput[0].value),
        maxVal = parseInt(experienceRangeInput[1].value);

        if((maxVal - minVal) < experienceGap){
            if(e.target.classList.contains("min-experience-range")){
                experienceRangeInput[0].value = maxVal - experienceGap
            }else{
                experienceRangeInput[1].value = minVal + experienceGap;
            }
        }else{
            experienceInput[0].value = minVal;
            experienceInput[1].value = maxVal;
            experienceProgress.style.left = ((minVal / experienceRangeInput[0].max) * 100) + "%";
            experienceProgress.style.right = 100 - (maxVal / experienceRangeInput[1].max) * 100 + "%";
        }
    });
});

document.addEventListener("DOMContentLoaded", function() {
    loadDropdownOptions()
});

async function loadDropdownOptions() {
    try {
        const response = await fetch('http://89.169.3.43/api/category')

        if (!response.ok) {
           throw new Error('Failed to load activity sectors') 
        }

        const sectors = await response.json()

        const optionsContainer = document.getElementById('options')
        optionsContainer.innerHTML = ''

        // Если нет данных, отображаем сообщение
        if (sectors.length === 0) {
            optionsContainer.innerHTML = '<p>Нет доступных сфер деятельности.</p>';
            return;
        }

        const fragment = document.createDocumentFragment();

        //динамически добавляем новые опции в дропдаун
        sectors.forEach(sector => {
            const label = document.createElement('label')
            label.setAttribute('for', `sector-${sector.id}`)

            const input = document.createElement('input')
            input.type = 'checkbox'
            input.value = sector.id
            input.id = `sector-${sector.id}`

            label.appendChild(input)

            const textNode = document.createTextNode(sector.name)
            label.appendChild(textNode)

            fragment.appendChild(label)
            });

            optionsContainer.appendChild(fragment);

    } catch (error) {
        console.error('Error loading activity sectors:', error)
        const optionsContainer = document.getElementById('options')
        optionsContainer.innerHTML = '<p>Не удалось загрузить сферы. Попробуйте еще раз позже.</p>'
    }
}

function toggleDropdown() {
    var options = document.getElementById("options");
    options.style.display = options.style.display === "block" ? "none" : "block";
}

document.addEventListener("click", function(event) {
    var dropdown = document.querySelector(".dropdown-container");
    if (!dropdown.contains(event.target)) {
        document.getElementById("options").style.display = "none";
    }
});

async function applySortingAndReload() {
    const sortBy = document.getElementById('variant').value
    const sortDirection = document.getElementById('route').value

    const url = new URL('http://89.169.3.43/api/consultant-cards')
    url.searchParams.append('sortBy', sortBy)
    url.searchParams.append('sortDirection', sortDirection)

    try {
        const respons = await fetch(url)
        if (respons.ok) {
            throw new Error("Ошибка при загрузке данных с сервера");
        }

        const data = await respons.json()
        createMentorCard(data)
    } catch (error) {
        console.error('Ошибка запроса', error)
    }
}

// показ и скрытие фильтра через кнопки
const openFilterButton = document.querySelector(".open-filter-button");
const closeFilterButton = document.querySelector(".close-filter-button");
const filterContainer = document.querySelector(".search-filter");
const body = document.body;

openFilterButton.addEventListener('click', () => {
    filterContainer.classList.add('display-block');
    body.classList.add('overflow-hidden');
});

closeFilterButton.addEventListener('click', () => {
    filterContainer.classList.remove('display-block');
    body.classList.remove('overflow-hidden');
});



// сброс фильтра для прогресса двойного ползунка
const filterResetButton = document.querySelector(".filter-reset-button");
filterResetButton.addEventListener('click', () => {
    experienceProgress.style.left = 0;
    experienceProgress.style.right = 0;
    priceProgress.style.left = 0;
    priceProgress.style.right = 0;
});

// Функция для обновления фильтров и отправки данных на сервер
function appFilters() {
    const minPrice = document.getElementById('min-price').value || 0
    const maxPrice = document.getElementById('max-price').value || 100000
    const minExperience = document.getElementById('min-experience').value || 0
    const maxExperience = document.getElementById('max-experience').value || 100
    const selectedSector = Array.from(document.querySelectorAll('.dropdown-options input:checked'))
        .map(checkbox => checkbox.value)
    const sortBy = document.getElementById('variant')?.value;
    const sortDirection = document.getElementById('route')?.value;

    const queryParams = new URLSearchParams()

    if (minPrice > 0) queryParams.append('startPrice', minPrice)
    if (maxPrice < 100000) queryParams.append('endPrice', maxPrice)
    if (minExperience > 0) queryParams.append('minTotalExperienceYears', minExperience)

    selectedSector.forEach(id => {
        queryParams.append('categoryIds', id)
    })

    if (sortBy) queryParams.append('sortBy', sortBy);
    if (sortDirection) queryParams.append('sortDirection', sortDirection);

    fetchResults(queryParams.toString())
}



async function fetchResults(filters) {
    try {
        const response = await fetch('http://89.169.3.43/api/consultant-cards?' + filters, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json()

        if (!Array.isArray(data)) {
            console.error("Ответ от сервера не является массивом:", data)
            return
        }

        updateResults(data)
    } catch (error) {
        console.error('Ошибка при получении данных:', error)
    }
}



// Функция для обновления результатов на странице
function updateResults(data) {
    const resultContainer = document.getElementById('mentor-card-container')

    if (!resultContainer) {
        console.error("Element with id 'result-container' not found")
        return
    }

    resultContainer.innerHTML = ''

    //Получаем значение maxExperience с формы
    const maxExperience = parseFloat(document.getElementById('max-experience').value) || 100

    //Фильтруем данные
    const filteredData = data.filter(mentor => {
        const totalExperience = mentor.experiences?.reduce((sum, exp) => sum + exp.durationYears, 0) || 0
        return totalExperience <= maxExperience
    })

    if (filteredData.length > 0) {
        filteredData.forEach(mentor => {
            const mentorCard = createMentorCard(mentor)
            resultContainer.appendChild(mentorCard)
        });
    } else {
        resultContainer.innerHTML = '<p>Нет подходящих результатов.</p>'
    }
}


document.addEventListener('DOMContentLoaded', function () {
    // Обработчик для кнопки "Применить фильтрацию"
    document.querySelector('.filter-submit-button').addEventListener('click', (e) => {
        e.preventDefault()  // Предотвращаем отправку формы
        appFilters()  // Применяем фильтры
    });

    // Обработчик для кнопки "Сбросить фильтрацию"
    document.querySelector('.filter-reset-button').addEventListener('click', (e) => {
        e.preventDefault()
        resetFilters()
        appFilters()
    });
});

function resetFilters() {
    document.getElementById('min-price').value = 0
    document.getElementById('max-price').value = 100000
    document.getElementById('min-experience').value = 0
    document.getElementById('max-experience').value = 100

    // Сброс значений range input для цен и опыта
    priceRangeInput[0].value = 0
    priceRangeInput[1].value = 100000
    experienceRangeInput[0].value = 0
    experienceRangeInput[1].value = 100

    // Обновляем прогресс-бар для цен и опыта
    priceProgress.style.left = 0
    priceProgress.style.right = 0
    experienceProgress.style.left = 0
    experienceProgress.style.right = 0


    document.querySelectorAll('.dropdown-options input').forEach(input => {
        input.checked = false
    });

    document.getElementById('variant').value = '';
    document.getElementById('route').value = '';
}

// // Функция для поиска менторов
document.addEventListener('DOMContentLoaded', function () {

    const searchForm = document.getElementById('search-form')
    const searchInput = document.getElementById('search')
    
    // Проверяем, что форма и поле поиска существуют на странице
    if (searchForm && searchInput) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault()
            alert('Submit intercepted')

            const query = searchInput.value.trim(); 

            if (query) {
                searchMentors(query)
            } else {
                console.log('Поиск не выполнен, строка поиска пуста');
            }
        });
    } else {
        console.error('Элементы для поиска не найдены на странице');
    }
});

async function searchMentors(query) {
    try {
        const queryParams = new URLSearchParams({
            searchTerm: query
        }).toString()

        const response = await fetch('http://89.169.3.43/api/consultant-cards?' + queryParams, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json()

        updateResultssearch(data)
    } catch (error) {
        console.error('Ошибка при поиске:', error)
    }
}

function updateResultssearch(data) {
    const resultContainer = document.getElementById('mentor-card-container')

    if (!resultContainer) {
        console.error("Element with id 'mentor-card-container' not found")
        return;
    }

    resultContainer.innerHTML = ''

    if (data && data.length > 0) {
        data.forEach(mentor => {
            const mentorCard = createMentorCard(mentor)
            resultContainer.appendChild(mentorCard)
        });
    } else {
        resultContainer.innerHTML = '<p>Нет подходящих результатов.</p>'
    }
}
