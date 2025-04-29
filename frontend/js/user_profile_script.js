function remove(button) {
    let card = button.closest('.mentor-card');
    if (card) {
        card.remove();
    }
}