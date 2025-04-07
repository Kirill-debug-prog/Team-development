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
