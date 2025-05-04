FROM nginx:latest

COPY ./nginx.conf /etc/nginx/nginx.conf

COPY ./frontend/html/ /usr/share/nginx/html/

COPY ./frontend/js/ /usr/share/nginx/html/js/

COPY ./frontend/fonts/ /usr/share/nginx/html/fonts/

COPY ./frontend/icons/ /usr/share/nginx/html/icons/

COPY ./frontend/images/ /usr/share/nginx/html/images/
EXPOSE 80

# Эта команда, скорее всего, уже есть по умолчанию в образе, но лучше явно указать
CMD ["nginx", "-g", "daemon off;"]