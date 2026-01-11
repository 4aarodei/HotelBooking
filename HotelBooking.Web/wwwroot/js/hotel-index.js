document.addEventListener('DOMContentLoaded', () => {
    const checkInInput = document.getElementById('filter-check-in');
    const checkOutInput = document.getElementById('filter-check-out');

    if (!checkInInput || !checkOutInput) {
        return;
    }

    const updateMinCheckOut = () => {
        if (!checkInInput.value) {
            return;
        }

        const checkInDate = new Date(checkInInput.value);
        checkInDate.setDate(checkInDate.getDate() + 1);
        const minCheckOut = checkInDate.toISOString().split('T')[0];

        checkOutInput.min = minCheckOut;

        if (checkOutInput.value && checkOutInput.value < minCheckOut) {
            checkOutInput.value = minCheckOut;
        }
    };

    checkInInput.addEventListener('change', updateMinCheckOut);
});