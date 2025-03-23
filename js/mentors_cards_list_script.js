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
        let minPrice = parseInt(experienceInput[0].value),
        maxPrice = parseInt(experienceInput[1].value);
        
        if((maxPrice - minPrice >= experienceGap) && (minPrice >= experienceRangeInput[0].min) && (maxPrice <= experienceRangeInput[1].max)){
            if(e.target.classList.contains("min-experience-input")){
                experienceRangeInput[0].value = minPrice;
                experienceProgress.style.left = ((minPrice / experienceRangeInput[0].max) * 100) + "%";
            }else{
                experienceRangeInput[1].value = maxPrice;
                experienceProgress.style.right = 100 - (maxPrice / experienceRangeInput[1].max) * 100 + "%";
            }
        }
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