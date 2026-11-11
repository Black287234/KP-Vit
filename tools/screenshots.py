# -*- coding: utf-8 -*-
"""
Ручные проверки через браузер и HTTP на сервере GitHub (workflow checks.yml) со скриншотами.

  python tools/screenshots.py before      — основные проверки
  python tools/screenshots.py after       — проверка после перезапуска приложения
  python tools/screenshots.py tests FILE  — картинка с выводом dotnet test

Фактические результаты читаются со страниц и из ответов сервера и пишутся в docs/screenshots/results.json.
"""
import html
import json
import sys
import time
from datetime import datetime, timedelta
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = "http://localhost:5090"
OUT = Path("docs/screenshots")
OUT.mkdir(parents=True, exist_ok=True)
RESULTS = OUT / "results.json"

NOW = datetime.now()
TOMORROW = (NOW + timedelta(days=1)).replace(hour=0, minute=0, second=0, microsecond=0)


def dt(day, h, m=0):
    return (day + timedelta(hours=h, minutes=m)).strftime("%Y-%m-%dT%H:%M")


def load():
    return json.loads(RESULTS.read_text(encoding="utf-8")) if RESULTS.exists() else {}


def save(r):
    RESULTS.write_text(json.dumps(r, ensure_ascii=False, indent=2), encoding="utf-8")


def shot(page, name):
    page.screenshot(path=str(OUT / f"{name}.png"), full_page=True)


def alerts(page):
    t = page.locator(".alert").all_inner_texts()
    return " | ".join(x.strip().replace("\n", " ") for x in t if x.strip())


def equipment_id(page, inv):
    data = page.request.get(BASE + "/api/equipment").json()
    return next(e["id"] for e in data if e["inventoryNumber"] == inv)


def book(page, eq_id, start, end, purpose="Занятие по расписанию", employee="1"):
    page.goto(BASE + f"/Bookings/Create?equipmentId={eq_id}")
    page.select_option("#Form_EmployeeId", employee)
    page.fill("#Form_StartAt", start)
    page.fill("#Form_EndAt", end)
    page.fill("#Form_Purpose", purpose)
    page.click("button:has-text('Забронировать')")
    page.wait_for_load_state()


def api_post(page, path, data):
    resp = page.request.post(BASE + path, data=data)
    try:
        body = resp.json()
    except Exception:
        body = resp.text()
    return resp.status, body


def before(page, r):
    page.goto(BASE)
    r["home"] = " / ".join(x.strip() for x in page.locator(".stat").all_inner_texts())
    shot(page, "01_home")

    page.goto(BASE + "/Equipment")
    r["catalog"] = f"карточек в каталоге: {page.locator('.card.h-100').count()}"
    shot(page, "02_catalog")
    page.goto(BASE + "/Equipment?Search=ноутбук")
    r["search"] = "; ".join(page.locator(".card h2").all_inner_texts())
    page.goto(BASE + "/Equipment?State=Maintenance")
    r["filter_state"] = "; ".join(page.locator(".card h2").all_inner_texts())

    projector = equipment_id(page, "INV-0001")
    doc_cam = equipment_id(page, "INV-0401")
    speaker = equipment_id(page, "INV-0302")
    tablet = equipment_id(page, "INV-0501")

    page.goto(BASE + f"/Equipment/Details/{projector}")
    r["equipment_card"] = f"броней в карточке INV-0001: {page.locator('tbody tr').count()}"
    shot(page, "03_equipment_card")

    # доступность на завтра 10:00–12:00
    page.goto(BASE + f"/Availability?from={dt(TOMORROW, 10)}&to={dt(TOMORROW, 12)}")
    r["availability"] = " | ".join(x.strip() for x in page.locator("h2").all_inner_texts())
    shot(page, "04_availability")
    page.goto(BASE + f"/Availability?from={dt(TOMORROW, 12)}&to={dt(TOMORROW, 10)}")
    r["availability_bad"] = alerts(page)

    # успешная бронь документ-камеры
    book(page, doc_cam, dt(TOMORROW, 10), dt(TOMORROW, 12), "Семинар: разбор документов")
    r["create_ok"] = alerts(page) + " → " + page.locator("h1").inner_text().strip()
    r["_created_url"] = page.url
    shot(page, "05_booking_created")

    # пересечение: проектор занят завтра 10–12 и 12–14
    book(page, projector, dt(TOMORROW, 11), dt(TOMORROW, 13))
    r["overlap"] = alerts(page)
    shot(page, "06_booking_overlap")
    book(page, projector, dt(TOMORROW, 13, 59), dt(TOMORROW, 16))
    r["overlap_1min"] = alerts(page)
    book(page, projector, dt(TOMORROW, 14), dt(TOMORROW, 15), "Консультация перед экзаменом")
    r["touch"] = alerts(page)

    book(page, doc_cam, dt(TOMORROW, 16), dt(TOMORROW, 15))
    r["end_before_start"] = alerts(page)
    today = NOW.replace(hour=0, minute=0, second=0, microsecond=0)
    book(page, doc_cam, dt(today, 0), dt(today, 1))
    r["past"] = alerts(page)
    book(page, doc_cam, dt(TOMORROW, 9), dt(TOMORROW + timedelta(days=15), 9))
    r["too_long"] = alerts(page)
    book(page, doc_cam, dt(TOMORROW, 17), dt(TOMORROW, 18), "")
    r["empty_purpose"] = alerts(page)

    s, b = api_post(page, "/api/bookings", {"equipmentId": speaker, "employeeId": 1, "purpose": "Концерт",
                                            "startAt": dt(TOMORROW, 10), "endAt": dt(TOMORROW, 12)})
    r["maintenance"] = f"HTTP {s}: {b.get('error') if isinstance(b, dict) else b}"
    s, b = api_post(page, "/api/bookings", {"equipmentId": tablet, "employeeId": 1, "purpose": "Урок",
                                            "startAt": dt(TOMORROW, 10), "endAt": dt(TOMORROW, 12)})
    r["written_off"] = f"HTTP {s}: {b.get('error') if isinstance(b, dict) else b}"

    # отмена брони
    page.goto(r["_created_url"])
    page.fill("#reason", "")
    page.click("button:has-text('Отменить бронь')")
    r["cancel_no_reason"] = alerts(page)
    page.fill("#reason", "Семинар перенесён на следующую неделю")
    page.click("button:has-text('Отменить бронь')")
    r["cancel_ok"] = alerts(page) + " → статус: " + page.locator("h1 .badge").inner_text().strip()
    shot(page, "07_booking_cancelled")
    booking_id = int(r["_created_url"].rstrip("/").split("/")[-1])
    s, b = api_post(page, f"/api/bookings/{booking_id}/cancel", {"reason": "Ещё раз"})
    r["cancel_twice"] = f"HTTP {s}: {b.get('error') if isinstance(b, dict) else b}"
    book(page, doc_cam, dt(TOMORROW, 10), dt(TOMORROW, 12), "Семинар: новая дата")
    r["after_cancel_free"] = alerts(page)

    # автоматическое завершение: бронь на ближайшую минуту
    start = datetime.now().replace(second=0, microsecond=0)
    s, b = api_post(page, "/api/bookings", {"equipmentId": doc_cam, "employeeId": 2, "purpose": "Короткая проверка",
                                            "startAt": start.strftime("%Y-%m-%dT%H:%M"),
                                            "endAt": (start + timedelta(minutes=1)).strftime("%Y-%m-%dT%H:%M")})
    short_id = b.get("id") if isinstance(b, dict) else None
    time.sleep(70)
    page.goto(BASE + f"/Bookings/Details/{short_id}")
    r["auto_complete"] = f"создана {s}; через 70 с статус: " + page.locator("h1 .badge").inner_text().strip() + \
                         "; журнал: " + " / ".join(x.split("·")[0].strip() for x in page.locator("li b").all_inner_texts())

    page.goto(BASE + "/Bookings?Tab=current")
    r["tab_current"] = page.locator("p.text-muted").first.inner_text().strip()
    shot(page, "08_bookings_current")
    page.goto(BASE + "/Bookings?Tab=finished")
    r["tab_finished"] = page.locator("p.text-muted").first.inner_text().strip()
    shot(page, "09_bookings_finished")
    page.goto(BASE + f"/Bookings?EmployeeId=2&Date={TOMORROW:%Y-%m-%d}")
    r["filter"] = page.locator("p.text-muted").first.inner_text().strip()

    page.goto(BASE + "/Schedule")
    r["schedule"] = f"строк оборудования: {page.locator('tbody tr').count()}, занятых отрезков: {page.locator('.slot').count()}"
    shot(page, "10_schedule")

    # каталог: добавление, повтор номера, смена состояния
    page.goto(BASE + "/Equipment/Edit")
    page.fill("#Item_InventoryNumber", "INV-0601")
    page.fill("#Item_Category", "Принтеры")
    page.fill("#Item_Name", "3D-принтер Bambu Lab A1")
    page.click("button:has-text('Сохранить')")
    r["equipment_create"] = alerts(page) + " → " + page.locator("h1").inner_text().strip()
    page.goto(BASE + "/Equipment/Edit")
    page.fill("#Item_InventoryNumber", "inv-0001")
    page.fill("#Item_Category", "Проекторы")
    page.fill("#Item_Name", "Проектор-дубль")
    page.click("button:has-text('Сохранить')")
    r["equipment_duplicate"] = alerts(page)
    shot(page, "11_equipment_duplicate")
    page.goto(BASE + f"/Equipment/Details/{projector}")
    page.click("button:has-text('На обслуживание')")
    r["state_with_bookings"] = alerts(page)

    resp = page.goto(BASE + "/Bookings/Details/999")
    r["not_found_page"] = f"HTTP {resp.status}: " + page.locator("h4").inner_text().strip()
    shot(page, "12_not_found")
    resp = page.request.get(BASE + "/api/equipment/999")
    r["not_found_api"] = f"HTTP {resp.status}: {resp.json().get('error')}"


def after(page, r):
    page.goto(BASE + "/Equipment?Search=INV-0601")
    found = page.locator(".card h2").all_inner_texts()
    page.goto(BASE + "/Bookings?Tab=current")
    current = page.locator("p.text-muted").first.inner_text().strip()
    r["after_restart"] = f"INV-0601 найден: {'да' if found else 'нет'}; текущие брони: {current} (до перезапуска: {r.get('tab_current')})"


def tests_image(page, text_file):
    text = Path(text_file).read_text(encoding="utf-8", errors="replace")
    lines = [l for l in text.splitlines() if l.strip()][-70:]
    page.set_content("<html><body style='margin:0;background:#0c0c0c'><pre style='font:13px Consolas,monospace;"
                     "color:#d0d0d0;padding:16px;white-space:pre-wrap'>runner@github-actions:~/KP-Vit$ dotnet test\n"
                     + html.escape("\n".join(lines)) + "</pre></body></html>")
    shot(page, "13_dotnet_test")


def main():
    mode = sys.argv[1]
    r = load()
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", args=["--lang=ru-RU"])
        page = browser.new_page(viewport={"width": 1280, "height": 800}, locale="ru-RU")
        if mode == "before":
            before(page, r)
        elif mode == "after":
            after(page, r)
        elif mode == "tests":
            tests_image(page, sys.argv[2])
        browser.close()
    save(r)


if __name__ == "__main__":
    main()
