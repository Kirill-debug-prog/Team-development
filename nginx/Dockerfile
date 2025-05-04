FROM nginx:latest

COPY ./nginx.conf /etc/nginx/nginx.conf

# Копируем содержимое папки html в корень директории Nginx для статики
COPY ../frontend/html/ /usr/share/nginx/html/

# Копируем содержимое папки js
COPY ../frontend/js/ /usr/share/nginx/html/js/

# Копируем содержимое папки fonts
COPY ../frontend/fonts/ /usr/share/nginx/html/fonts/

# Копируем содержимое папки icons
COPY ../frontend/icons/ /usr/share/nginx/html/icons/

# Копируем содержимое папки images
COPY ../frontend/images/ /usr/share/nginx/html/images/

EXPOSE 80

# Эта команда, скорее всего, уже есть по умолчанию в образе, но лучше явно указать
CMD ["nginx", "-g", "daemon off;"]