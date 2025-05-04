FROM nginx:latest

COPY ./nginx.conf /etc/nginx/nginx.conf
# Копируем все содержимое папки frontend в директорию Nginx для статики
# ВНИМАНИЕ: Читайте пояснения ниже, почему это может не работать как вы ожидаете
COPY ./frontend/ /usr/share/nginx/html/

EXPOSE 80

# Эта команда, скорее всего, уже есть по умолчанию в образе, но лучше явно указать
CMD ["nginx", "-g", "daemon off;"]