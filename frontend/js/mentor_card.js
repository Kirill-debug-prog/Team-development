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