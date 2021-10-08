from flask import Flask, jsonify, send_file, request, render_template, redirect
from apscheduler.schedulers.background import BackgroundScheduler
import pymysql
import requests
import json
import time

app = Flask(__name__)

def execute_mysql_nonquery(sql):
    '''执行sql语句
    sql:    sql语句
    '''
    db = pymysql.connect(host = "localhost", port = 3306, user = "root", passwd = "root",
                     database = "buptrushreport", charset = "utf8")
    cursor = db.cursor()
    try:
        cursor.execute(sql)
        db.commit()
    except:
        db.rollback()
    db.close()

def execute_mysql_query(sql):
    '''执行sql查询语句
    sql:    sql语句
    '''
    db = pymysql.connect(host = "localhost", port = 3306, user = "root", passwd = "root",
                     database = "buptrushreport", charset = "utf8")
    cursor = db.cursor()
    results = None
    try:
        cursor.execute(sql)
        results = cursor.fetchall()
        db.commit()
    except:
        db.rollback()
    db.close()
    return results

def get_usersinfo():
    ''' 获取所有用户信息
    '''
    sql_userinfo = "SELECT * FROM user"
    userinfo_result = execute_mysql_query(sql_userinfo)
    userinfo = userinfo_result
    return userinfo

def get_userinfo(guid):
    ''' 获取单个用户信息
    '''
    sql_userinfo = "SELECT * FROM user WHERE GUID=\"%s\"" % (guid)
    userinfo_result = execute_mysql_query(sql_userinfo)
    userinfo = userinfo_result
    return userinfo

def reg_userinfo(guid, username, password, cookie, userchk):
    execute_mysql_nonquery("UPDATE user SET active=1, username=\"%s\", password=\"%s\", cookie=\"%s\", usePWD=%d WHERE GUID = \"%s\"" % (username, password, cookie, userchk, guid))
    return True

def add_userinfo(guid, isadmin):
    execute_mysql_nonquery("INSERT INTO user(GUID, isAdmin) VALUES(\"%s\", %d)" % (guid, isadmin))
    return True

def remove_userinfo(guid):
    execute_mysql_nonquery("DELETE FROM user WHERE GUID=\"%s\"" % (guid))
    return True

def verify(username, password):
    url_check = "https://app.bupt.edu.cn/uc/wap/login/check"
    header = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0",
        "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
        }
    with requests.post(url_check, headers = header, data = "username=%s&password=%s" % (username, password)) as response:
        if "\"e\":0" in response.text:
            set_cookie = response.headers["Set-Cookie"]
            return set_cookie
        else:
            return None

def save(cookie):
    url_index = "https://app.bupt.edu.cn/ncov/wap/default/index"
    url_save = "https://app.bupt.edu.cn/ncov/wap/default/save"
    header = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0",
        "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
        "Cookie": cookie
        }
    with requests.get(url_index, headers = header) as response:
        resp = response.text
    if "hasFlag: '1'" in resp:
        return {"e": 0, "m": "今天已经填报了"}
    elif "<title>登录</title>" in resp:
        return {"e": 1, "m": "登录失败"}
    else:
        pattern_start = "oldInfo: "
        old_info = resp[(resp.index(pattern_start) + len(pattern_start)):]
        old_info = old_info[:old_info.index("tipMsg: ''")].strip()
        old_info = old_info[:-1]
        data = json.loads(old_info)
        with requests.post(url_save, headers = header, data = data) as response:
            resp = response.text
        resp_data = json.loads(resp)
        return {"e": resp_data["e"], "m": resp_data["m"]}

def do_task():
    users = get_usersinfo()
    user_info = []
    for user in users:
        if user[-2] == 1:
            if user[5] == 1:
                user_info.append([user[1], user[2], user[3]])
            else:
                user_info.append([user[1], user[4]])
    for user in user_info:
        if len(user) == 3:
            cookie = verify(user[1], user[2])
        elif len(user) == 2:
            cookie = user[1]
        else:
            continue
        last_time = time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(time.time()))
        guid = user[0]
        if not cookie:
            execute_mysql_nonquery("UPDATE user SET lastTime = \"%s\", result = 1, message = \"用户身份验证失败\" WHERE GUID = \"%s\"" % (last_time, guid))
        else:
            save_result = save(cookie)
            execute_mysql_nonquery("UPDATE user SET lastTime = \"%s\", result = %d, message = \"%s\" WHERE GUID = \"%s\"" % (last_time, save_result["e"], save_result["m"], guid))


@app.route("/api/user/<string:guid>", methods = ["GET"])
def get_user(guid):
    single_user = get_userinfo(guid)
    if len(single_user) > 0:
        user = single_user[0]
        if user[9] == 0:
            return jsonify({"success": 1, "message": "Not Activated", "data": None})
        return jsonify({"success": 0, "message": "", "data": {
            "guid": user[1],
            "lastTime": user[6],
            "result": user[7],
            "message": user[8]
            }})
    else:
        return jsonify({"success": 1, "message": "No Such User", "data": None})

@app.route("/api/user/<string:guid>", methods = ["POST"])
def reg_user(guid):
    single_user = get_userinfo(guid)
    if len(single_user) == 0:
        return jsonify({"success": 1, "message": "No Such User", "data": None})
    elif single_user[0][-2] == 1:
        return jsonify({"success": 1, "message": "Already Activated", "data": None})
    else:
        v_username = request.values.get("username")
        v_password = request.values.get("password")
        v_cookie = request.values.get("cookie")
        v_userchk = 1 if request.values.get("userchk") == "usePWD" else 0
        ret = reg_userinfo(guid, v_username, v_password, v_cookie, v_userchk)
        if ret:
            return jsonify({"success": 0, "message": "OK", "data": None})
        else:
            return jsonify({"success": 1, "message": "Failed", "data": None})

@app.route("/api/users/<string:guid>", methods = ["GET"])
def get_all_user(guid):
    all_user = get_usersinfo()
    is_admin = False
    for user in all_user:
        if user[1] == guid and user[-1] == 1:
            is_admin = True
            break
    user_list = []
    for user in all_user:
        user_list.append({
            "guid": user[1],
            "lastTime": user[6],
            "result": user[7],
            "message": user[8],
            "active": user[9],
            "isAdmin": user[10]
            })
    if is_admin:
        return jsonify({"success": 0, "message": "", "data": user_list})
    else:
        return jsonify({"success": 1, "message": "Not Admin", "data": None})

@app.route("/api/users/<string:guid>", methods = ["POST"])
def add_user(guid):
    v_id = request.values.get("id")
    v_isAdmin = request.values.get("isAdmin")
    single_user = get_userinfo(guid)
    if len(single_user) == 0 or single_user[0][10] != 1:
        return jsonify({"success": 1, "message": "Not Permitted", "data": None})
    target_user = get_userinfo(v_id)
    if len(target_user) > 0:
        return jsonify({"success": 1, "message": "Duplicated", "data": None})
    ret = add_userinfo(v_id, 1 if v_isAdmin == "true" else 0)
    if ret:
        return jsonify({"success": 0, "message": "OK", "data": None})
    else:
        return jsonify({"success": 1, "message": "Failed", "data": None})

@app.route("/api/users/remove/<string:guid>", methods = ["POST"])
def remove_user(guid):
    v_id = request.values.get("id")
    single_user = get_userinfo(guid)
    if len(single_user) == 0 or single_user[0][10] != 1:
        return jsonify({"success": 1, "message": "Not Permitted", "data": None})
    target_user = get_userinfo(v_id)
    if len(target_user) == 0:
        return jsonify({"success": 1, "message": "No Such User", "data": None})
    ret = remove_userinfo(v_id)
    if ret:
        return jsonify({"success": 0, "message": "OK", "data": None})
    else:
        return jsonify({"success": 1, "message": "Failed", "data": None})

@app.route("/", methods = ["GET"])
def index():
    return send_file("./index.html")

@app.route("/<string:guid>", methods = ["GET"])
def search(guid):
    single_user = get_userinfo(guid)
    if len(single_user) == 0:
        return redirect('https://cn.bing.com/search?q=' + guid)
    user = single_user[0]
    if user[9] == 0:
        return render_template("./reg.html", GUID = guid)
    elif user[10] == 1:
        return render_template("./Admin.html", GUID = guid)
    else:
        return render_template("./Normal.html", GUID = guid)

scheduler = BackgroundScheduler()
scheduler.add_job(do_task, 'cron', day_of_week='0-6', hour=0, minute=5)
scheduler.start()

app.run("0.0.0.0", port=5555)