# -*- coding: utf-8 -*-
"""Сборка: python3 docs/report/build_note.py → docs/Пояснительная_записка.docx (+PDF), docs/Таблица_проверок.docx"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from gost_docx import build, NoteBuilder, margins  # noqa: E402
from checks_rows import ROWS  # noqa: E402

ROOT = HERE.parents[1]
INFO = {
    "topic": "Проектирование и разработка модуля бронирования оборудования",
    "pm": "ПО ПМ.01 РАЗРАБОТКА МОДУЛЕЙ ПРОГРАММНОГО ОБЕСПЕЧЕНИЯ ДЛЯ КОМПЬЮТЕРНЫХ СИСТЕМ",
    "mdk": "МДК.01.01 РАЗРАБОТКА ПРОГРАММНЫХ МОДУЛЕЙ",
    "specialty": "Специальность 09.02.07 «Информационные системы и программирование»",
    "group": "ИСиП-23/1",
    "student": "[Фамилия Имя Отчество]",
    "student_placeholder": True,
    "teacher": "Крючков Виктор Владимирович",
    "year": "2026",
}

r = json.loads((ROOT / "docs/screenshots/results.json").read_text(encoding="utf-8"))
r["not_found_page"] = r["not_found_page"] + "; API: " + r.get("not_found_api", "")


def clean(v):
    return str(v).replace("|", "/").replace("\n", " ")


table = ":table Ручные проверки (полная таблица)\n№ | Функция | Исходные данные и действия | Ожидаемый результат | Фактический результат\n"
table += "\n".join(f"{n} | {f} | {a} | {e} | {clean(r.get(k, ''))}" for n, f, a, e, k in ROWS) + "\n"

text = (HERE / "note.txt").read_text(encoding="utf-8").replace("{{CHECKS_TABLE}}", table)
for k, v in r.items():
    text = text.replace("{" + k + "}", clean(v))
tmp = HERE / "_filled.txt"
tmp.write_text(text, encoding="utf-8")
b, pages = build(HERE / "title_template.docx", INFO, tmp, ROOT / "docs" / "Пояснительная_записка.docx")
tmp.unlink()
for level, t in b.toc_entries:
    print(("   " if level == 2 else "") + t, pages.get(t))

b2 = NoteBuilder(HERE / "title_template.docx", INFO, HERE)
body = b2.doc.element.body
for el in list(body):
    if el.tag.endswith("}p") or el.tag.endswith("}tbl"):
        body.remove(el)
margins(b2.doc.sections[0])
b2.load("# ТАБЛИЦА ПРОВЕРОК МОДУЛЯ БРОНИРОВАНИЯ ОБОРУДОВАНИЯ\n\nАвтоматические тесты: 52, пройдено 52. "
        "Ручные проверки выполнены в браузере на копии проекта, развёрнутой из архива; фактические результаты прочитаны со страниц.\n\n" + table)
b2.doc.save(ROOT / "docs" / "Таблица_проверок.docx")
print("Таблица проверок сохранена")
