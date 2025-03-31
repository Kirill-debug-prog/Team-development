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
