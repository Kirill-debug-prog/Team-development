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

function redirectToLogin() {
    localStorage.setItem('expiredMessage', 'Ваша сессия устарела.');

    localStorage.removeItem('firstName');
	localStorage.removeItem('lastName');
	localStorage.removeItem('patronymic');
    localStorage.removeItem('id');

    window.location.href = './login.html';
}



let experienceCount = 1;
if (document.querySelector('.modify-card-form')) {
    experienceCount = 0;
}


function addExperience(position='', companyName='', durationYears='', experienceId='') {
    experienceCount++;
    let container = document.getElementById("experience-list");
    let newExperience = document.createElement("div");
    newExperience.classList.add("experience-item");
    newExperience.setAttribute('data-experience-id', experienceId);
    
    newExperience.innerHTML = `
        <div class="experience-title-button-wrapper">
            <span class="experience-item-title">Опыт работы №${experienceCount}</span>
            <button type="button" class="remove-experience-button" onclick="removeExperience(this)">×</button>
        </div>
        
        <label for="position-${experienceCount}" class="visually-hidden">Должность</label>
        <input id="position-${experienceCount}" class="input" type="text" value="${position}" placeholder="Должность" minlength="1" maxlength="255" required>
        
        <label for="company-${experienceCount}" class="visually-hidden">Компания</label>
        <input id="company-${experienceCount}" class="input" type="text" value="${companyName}" placeholder="Компания" minlength="1" maxlength="255" required>
        
        <label for="duration-${experienceCount}" class="visually-hidden">Срок работы</label>
        <input id="duration-${experienceCount}" class="input" type="number" value="${durationYears}" placeholder="Срок работы (например, 2 года)"  min="1" max="100" required>                                           
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

const pickedFieldOptionsDisplay = document.querySelector(".picked-field-options");
document.querySelector(".dropdown-options").addEventListener('change', () => {
    const checkedInputs = document.querySelector(".dropdown-options").querySelectorAll("input:checked");
    const pickedOptions = Array.from(checkedInputs).map(function (checkedInput) {
        return checkedInput.closest('label').textContent;
    })

    pickedFieldOptionsDisplay.innerHTML = `${pickedOptions.join(', ')}`;
})


function toggleOptionsDisplay() {
    document.querySelector(".picked-field-options").classList.toggle("hide-options");
}


// отправка всех форм
document.addEventListener('DOMContentLoaded', async () => {
    const form = document.querySelector('.form')

    form.addEventListener('submit', async (e) => {
        e.preventDefault()

        const title = document.getElementById('specialization').value.trim()
        const price = parseInt(document.getElementById('price').value, 10)
        const description = document.getElementById('description').value.trim()

        const mentorId = localStorage.getItem('id')

        const experiences = [];
        document.querySelectorAll('.experience-item').forEach(item => {
            const positionInput = item.querySelector('input[placeholder="Должность"]')
            const companyInput = item.querySelector('input[placeholder="Компания"]')
            const durationInput = item.querySelector('input[placeholder^="Срок работы"]')
            
            const position = positionInput ? positionInput.value.trim() : ''
            const companyName = companyInput ? companyInput.value.trim() : ''
            const durationValue = durationInput ? durationInput.value.trim() : ''
            const durationYears = parseInt(durationValue, 10);
            const id = item.getAttribute('data-experience-id');

            if (position && companyName && !isNaN(durationYears) && durationYears > 0) {
                
                if (document.querySelector('.modify-card-form')) {
                    experiences.push({
                        id,
                        position,
                        companyName,
                        durationYears,
                    })
                } else {
                    experiences.push({
                        position,
                        companyName,
                        durationYears,
                    })
                }
                
            }
        })

        // if (experiences.length === 0) {
        //     alert('Пожалуйста, добавьте хотя бы один опыт работы')
        //     return
        // }

        const selectedCategoryIds = Array.from(document.querySelectorAll('.dropdown-options input:checked'))
            .map(input => parseInt(input.value, 10))
            .filter(id => !isNaN(id));

        // if (selectedCategoryIds.length === 0) {
        //     alert('Пожалуйста, добавьте хотя бы одну сферу деятельности')
        //     return
        // }

        const payload = {
            title, 
            mentorId, 
            description,
            pricePerHours: price,
            experiences, 
            selectedCategoryIds
        };

        try {
            console.log('Отправляемые данные:', payload);
            
            let api = 'http://89.169.3.43/api/consultant-cards';
            let method = 'POST';
            
            if (document.querySelector('.modify-card-form')) {
                const urlParams = new URLSearchParams(window.location.search);
                const id = urlParams.get('id');

                api = `http://89.169.3.43/api/consultant-cards/${id}`;
                method = 'PUT';
            }

            const response = await fetch(`${api}`, {
                method: `${method}`,
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getCookie('token')}`
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                showingSuccess.showModal();
            } else if(response.status === 401) {
                redirectToLogin();
            } else {
                const error = await response.json();
                console.log(response)
                console.log(error)
                showingError.querySelector(".dialog-text").innerHTML = `Ошибка: ${error.message || 'не удалось сохранить анкету'}`
                showingError.showModal();
            }
        } catch (error) {
            console.error('Ошибка при создании анкеты:', error)

            showingError.querySelector(".dialog-text").innerHTML = 'Произошла ошибка при сохранении анкеты.'
            showingError.showModal();
        }
    })

    await loadDropdownOptions()

    if (document.querySelector('.modify-card-form')) {
        const urlParams = new URLSearchParams(window.location.search)
        const id = urlParams.get('id')

        if (!id) {
            document.body.innerHTML = `No ID provided in the URL`;
            return;
        }

        await loadCard(id)
    }
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



async function loadCard(id) {
    try {
        const token = getCookie('token');
        if (!token) {
            redirectToLogin();
        }

        const response = await fetch(`http://89.169.3.43/api/consultant-cards/${id}`, {
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

        const userCardData = await response.json();
        addCard(userCardData);
        
    } catch (error) {
        document.body.innerHTML = `Ошибка "${error.message}". Попробуйте перезагрузить страницу`;
    }
}

function addCard({title, experiences, categories, description, pricePerHours}) {
    document.getElementById('specialization').value = title;
    document.getElementById('price').value = pricePerHours;
    document.getElementById('description').value = description;

    const experienceList = document.getElementById('experience-list');

    if (experiences.length) {
        experiences.forEach(experience => {
            addExperience(experience.position, experience.companyName, experience.durationYears, experience.id);
        })
    }

    if (categories.length) {
        categories.forEach(category => {
            document.getElementById(`sector-${category.id}`).checked = true;
        })
        const event = new Event('change');
        document.querySelector(".dropdown-options").dispatchEvent(event);
    }

}