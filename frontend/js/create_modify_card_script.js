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

function logout() {
    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    document.cookie = `token=${getCookie('token')};max-age=-1`;

    window.location.href = './mentors_cards_list.html';
}

function getCookie(name) {
  for (const entryStr of document.cookie.split('; ')) {
    const [entryName, entryValue] = entryStr.split('=');

    if (decodeURIComponent(entryName) === name) {
        return entryValue;
    }
  }
}



let experienceCount = 1;

function addExperience() {
    experienceCount++;
    let container = document.getElementById("experience-list");
    let newExperience = document.createElement("div");
    newExperience.classList.add("experience-item");
    
    newExperience.innerHTML = `
        <div class="experience-title-button-wrapper">
            <span class="experience-item-title">Опыт работы №${experienceCount}</span>
            <button type="button" class="remove-experience-button" onclick="removeExperience(this)">×</button>
        </div>
        
        <label for="position-${experienceCount}" class="visually-hidden">Должность</label>
        <input id="position-${experienceCount}" class="input" type="text" placeholder="Должность">
        
        <label for="company-${experienceCount}" class="visually-hidden">Компания</label>
        <input id="company-${experienceCount}" class="input" type="text" placeholder="Компания">
        
        <label for="duration-${experienceCount}" class="visually-hidden">Срок работы</label>
        <input id="duration-${experienceCount}" class="input" type="text" placeholder="Срок работы (например, 2 года)">                                           
    `;
    

    
    container.appendChild(newExperience);
}

function removeExperience(button) {
    let experienceBlock = button.closest('.experience-item');
    if (experienceBlock) {
        experienceBlock.remove();
        experienceCount--;

        // обновление номеров оставшихся блоков
        let experiences = document.querySelectorAll('.experience-item');
        experiences.forEach((experience, index) => {
            experience.querySelector('.experience-item-title').textContent = `Опыт работы №${index + 1}`;

            for (let inputType of ['position', 'company', 'duration']) {
                let label = experience.querySelector(`label[for^="${inputType}-"]`);
                let input = experience.querySelector(`input[id^="${inputType}-"]`);

                if(label && input){
                    label.setAttribute('for', `${inputType}-${index + 1}`);
                    input.id = `${inputType}-${index + 1}`;
                }
            }
        });
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


function getCookie(name) {
  for (const entryStr of document.cookie.split('; ')) {
    const [entryName, entryValue] = entryStr.split('=')

    if (decodeURIComponent(entryName) === name) {
        return entryValue;
    }
  }
}

document.addEventListener('DOMContentLoaded', () => {
    const form = document.querySelector('.form')

    form.addEventListener('submit', async (e) => {
    e.preventDefault()

    const title = document.getElementById('specialization').value.trim()
    const price = parseInt(document.getElementById('price').value, 10)
    const description = document.getElementById('description').value.trim()

    const experiences = [];
    document.querySelectorAll('.experience-item').forEach(item => {
        const positionInput = item.querySelector('input[placeholder="Должность"]')
        const companyInput = item.querySelector('input[placeholder="Компания"]')
        const durationInput = item.querySelector('input[placeholder^="Срок работы"]')
        
        const position = positionInput ? positionInput.value.trim() : ''
        const companyName = companyInput ? companyInput.value.trim() : ''
        const durationValue = durationInput ? durationInput.value.trim() : ''
        const durationYears = parseInt(durationValue, 10);
        
        if (position && companyName && !isNaN(durationYears) && durationYears > 0) {
            experiences.push({
                position,
                companyName,
                durationYears,
            })
        }
    })

    if (experiences.length === 0) {
        alert('Пожалуйста, добавьте хотя бы один опыт работы')
        return
    }

    const selectedCategoryIds = Array.from(document.querySelectorAll('.dropdown-options input:checked'))
        .map(input => parseInt(input.value, 10))
        .filter(id => !isNaN(id));

    if (selectedCategoryIds.length === 0) {
        alert('Пожалуйста, добавьте хотя бы одну сферу деятельности')
        return
    }

    const payload = {
        title, 
        price, 
        description,
        pricePerHours: price,
        experiences, 
        selectedCategoryIds
    };

    try {
        console.log('Отправляемые данные:', payload);
        const response = await fetch('http://89.169.3.43/api/consultant-cards', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getCookie('token')}`
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const created = await response.json();
            alert('Анкета успешно создана!');
            window.location.href = `./user_profile.html?id=${created.id}`
        } else {
            const error = await response.json();
            alert(`Ошибка: ${error.message || 'Не удалось сохранить анкету'}`)
        }
    } catch (error) {
        console.error('Ошибка при создании анкеты:', error)
        alert('Произошла ошибка при сохранении анкеты.')
    }
    })

    loadDropdownOptions()
})

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