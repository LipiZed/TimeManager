// Основные функции для работы с расписанием
document.addEventListener('DOMContentLoaded', function () {
    // Инициализация переменных и обработчиков
    initializeScheduleApp();

    // Обработка формы добавления задачи
    const saveObjectiveBtn = document.getElementById('saveObjectiveBtn');
    if (saveObjectiveBtn) {
        saveObjectiveBtn.addEventListener('click', submitAddObjectiveForm);
    }

    // Обработка формы редактирования задачи
    const updateObjectiveBtn = document.getElementById('updateObjectiveBtn');
    if (updateObjectiveBtn) {
        updateObjectiveBtn.addEventListener('click', submitEditObjectiveForm);
    }

    // Обработка чекбокса для напоминания
    const addReminderCheckbox = document.getElementById('AddReminder');
    if (addReminderCheckbox) {
        addReminderCheckbox.addEventListener('change', toggleReminderSection);
    }

    // Подсветка текущего часа на временной шкале
    highlightCurrentHour();
});

// Инициализация приложения расписания
function initializeScheduleApp() {
    // Добавляем обработчики к кнопкам редактирования и удаления
    const editButtons = document.querySelectorAll('.edit-objective');
    const deleteButtons = document.querySelectorAll('.delete-objective');

    editButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.stopPropagation(); // Предотвращаем открытие деталей
            const objectiveId = this.getAttribute('data-objective-id');
            prepareEditObjective(objectiveId);
        });
    });

    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.stopPropagation(); // Предотвращаем открытие деталей
            const objectiveId = this.getAttribute('data-objective-id');
            prepareDeleteObjective(objectiveId);
        });
    });

    // Подготавливаем модальное окно для деталей задачи
    const objectiveDetailsModal = document.getElementById('objectiveDetailsModal');
    if (objectiveDetailsModal) {
        objectiveDetailsModal.addEventListener('show.bs.modal', function (event) {
            const objectiveId = event.relatedTarget.getAttribute('data-objective-id');
            loadObjectiveDetails(objectiveId);
        });
    }

    // Кнопки в модальном окне деталей
    const editObjectiveBtn = document.getElementById('editObjectiveBtn');
    const deleteObjectiveBtn = document.getElementById('deleteObjectiveBtn');
    const completeObjectiveBtn = document.getElementById('completeObjectiveBtn');

    if (editObjectiveBtn) {
        editObjectiveBtn.addEventListener('click', function () {
            const objectiveId = this.getAttribute('data-objective-id');
            prepareEditObjective(objectiveId);
        });
    }

    if (deleteObjectiveBtn) {
        deleteObjectiveBtn.addEventListener('click', function () {
            const objectiveId = this.getAttribute('data-objective-id');
            prepareDeleteObjective(objectiveId);
        });
    }

    if (completeObjectiveBtn) {
        completeObjectiveBtn.addEventListener('click', function () {
            const objectiveId = this.getAttribute('data-objective-id');
            markObjectiveAsCompleted(objectiveId);
        });
    }

    // Устанавливаем текущую дату по умолчанию для полей дат
    setDefaultDateTimeValues();
}

// Функция для отображения деталей задачи
function showObjectiveDetails(objectiveId) {
    // Находим задачу по ID
    const objectiveElement = document.querySelector(`.objective-item[data-objective-id="${objectiveId}"]`);
    if (!objectiveElement) return;

    // Получаем данные из атрибутов
    const title = objectiveElement.querySelector('.objective-title').innerText;
    const timeText = objectiveElement.querySelector('.objective-time').innerText;

    // Получаем дополнительные данные через AJAX (здесь нужно будет реализовать метод в контроллере)
    // Для демонстрации используем простое заполнение
    document.getElementById('objectiveDetailsTitle').innerText = title;
    document.getElementById('objectiveDetailsTime').innerText = timeText;

    // Сохраняем ID задачи для кнопок
    document.getElementById('editObjectiveBtn').setAttribute('data-objective-id', objectiveId);
    document.getElementById('deleteObjectiveBtn').setAttribute('data-objective-id', objectiveId);
    document.getElementById('completeObjectiveBtn').setAttribute('data-objective-id', objectiveId);

    // Открываем модальное окно
    const modal = new bootstrap.Modal(document.getElementById('objectiveDetailsModal'));
    modal.show();
}

// Функция для загрузки деталей задачи через AJAX
function loadObjectiveDetails(objectiveId) {
    // Здесь должен быть AJAX запрос к серверу для получения полных данных о задаче
    // Для демонстрации используем простое заполнение из данных элемента
    const objectiveElement = document.querySelector(`.objective-item[data-objective-id="${objectiveId}"]`);
    if (!objectiveElement) return;

    const title = objectiveElement.querySelector('.objective-title').innerText;
    const timeText = objectiveElement.querySelector('.objective-time').innerText;

    document.getElementById('objectiveDetailsTitle').innerText = title;
    document.getElementById('objectiveDetailsTime').innerText = timeText;
    document.getElementById('objectiveDetailsDescription').innerText = 'Загрузка описания...';
    document.getElementById('objectiveStatusBadge').innerText = 'В процессе';
    document.getElementById('objectiveStatusBadge').className = 'badge bg-primary';

    // В реальном приложении здесь был бы AJAX запрос для получения полных данных
    // fetch(`/Schedule/GetObjectiveDetails/${objectiveId}`)
    //     .then(response => response.json())
    //     .then(data => {
    //         document.getElementById('objectiveDetailsDescription').innerText = data.description;
    //         // и т.д.
    //     });
}

// Функция для подготовки формы редактирования задачи
function prepareEditObjective(objectiveId) {
    // Закрываем модальное окно с деталями, если оно открыто
    const detailsModal = bootstrap.Modal.getInstance(document.getElementById('objectiveDetailsModal'));
    if (detailsModal) {
        detailsModal.hide();
    }

    // Находим задачу по ID
    const objectiveElement = document.querySelector(`.objective-item[data-objective-id="${objectiveId}"]`);
    if (!objectiveElement) return;

    // Заполняем форму редактирования
    document.getElementById('editObjectiveId').value = objectiveId;
    document.getElementById('editTitle').value = objectiveElement.querySelector('.objective-title').innerText;

    // Устанавливаем время начала и окончания (из data-атрибутов)
    const startTime = objectiveElement.getAttribute('data-start-time');
    const endTime = objectiveElement.getAttribute('data-end-time');

    document.getElementById('editStartTime').value = startTime;
    if (endTime) {
        document.getElementById('editEndTime').value = endTime;
    }

    // В реальном приложении здесь был бы AJAX запрос для получения полных данных

    // Открываем модальное окно редактирования
    const editModal = new bootstrap.Modal(document.getElementById('editObjectiveModal'));
    editModal.show();
}

// Функция для подтверждения