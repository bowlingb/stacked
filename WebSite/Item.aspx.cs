﻿using System;
using Entities;
using NHibernate.Expression;
using Ra.Widgets;
using Ra;

public partial class Item : System.Web.UI.Page
{
    private QuizItem _question;

    protected override void OnInit(EventArgs e)
    {
        GetQuestion();
        FillOutContent();
        SetCssClassIfCurrentOperatorHasVoted();
        SetCssClassForFavorite();
        DataBindTags();
        base.OnInit(e);
    }

    private void DataBindTags()
    {
        if (!IsPostBack)
        {
            repTags.DataSource = _question.Tags;
            repTags.DataBind();
        }
    }

    protected void quoteQuestion_Click(object sender, EventArgs e)
    {
        answerBody.Text = _question.BodyQuote;
        answerBody.Select();
        answerBody.Focus();
    }

    protected override void OnPreRender(EventArgs e)
    {
        // Only visible if some threshold has been reached...
        deleteQuestion.Visible = Operator.Current != null && 
            (Operator.Current.CanDeleteQuestion || Operator.Current.ID == _question.CreatedBy.ID);
        editQuestionBtn.Visible = Operator.Current != null && 
            (Operator.Current.CanEditQuestion || Operator.Current.ID == _question.CreatedBy.ID);

        answerQuestion.Visible = Operator.Current != null;
        quoteQuestion.Visible = Operator.Current != null;
        changeOrdering.Visible = _question.Children.Count > 1;
        base.OnPreRender(e);
    }

    protected void EditQuestionBtnClick(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        foreach (System.Web.UI.Control idx in btn.Parent.Controls)
        {
            if (idx is Panel)
            {
                Panel tmp = idx as Panel;
                if (tmp.CssClass == "editAnswer")
                {
                    int id = GetIdOfAnswer(btn);
                    TextArea txt = tmp.Controls[1] as TextArea;
                    txt.Text = QuizItem.Find(id).Body;
                    tmp.Visible = true;
                    new EffectFadeIn(tmp, 500)
                        .ChainThese(new EffectFocusAndSelect(txt))
                        .Render();
                }
            }
        }
    }

    protected void SaveAnswer(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        Panel pnl = btn.Parent as Panel;
        TextArea txt = pnl.Controls[1] as TextArea;
        new EffectFadeOut(pnl, 500).Render();
        int id = GetIdOfAnswer(pnl);
        QuizItem item = QuizItem.Find(id);
        item.Body = txt.Text;
        item.Save();
        _question.Refresh();
        DataBindAnswers();
    }

    public QuizItem.OrderAnswersBy OrderAnswersBy
    {
        get
        {
            if (ViewState["OrderAnswersBy"] == null)
                return QuizItem.OrderAnswersBy.Determine;
            return (QuizItem.OrderAnswersBy)ViewState["OrderAnswersBy"];
        }
        set
        {
            ViewState["OrderAnswersBy"] = value;
        }
    }

    private void DataBindAnswers()
    {
        answers.DataSource = _question.GetAnswers(OrderAnswersBy);
        answers.DataBind();
        answersWrapper.ReRender();
    }

    protected void DeleteQuestionBtnClick(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        int id = GetIdOfAnswer(btn);
        QuizItem.Find(id).Delete();
        DataBindAnswers();
    }

    protected void star_Click(object sender, EventArgs e)
    {
        try
        {
            // Clicking the star TOGGLES favorites, meaning if it's there it'll be deleted and vice versa...
            Favorite favorite = Favorite.FindFirst(
                Expression.Eq("FavoredBy", Operator.Current),
                Expression.Eq("Question", _question));
            if (favorite != null)
                favorite.Delete();
            else
            {
                Favorite f = new Favorite();
                f.FavoredBy = Operator.Current;
                f.Question = _question;
                f.Save();
            }
            Highlight(star);
            SetCssClassForFavorite();
        }
        catch (Exception err)
        {
            ShowError(err.Message);
        }
    }

    private void SetCssClassForFavorite()
    {
        if (Favorite.FindFirst(
            Expression.Eq("FavoredBy", Operator.Current),
            Expression.Eq("Question", _question)) != null)
        {
            star.CssClass = "starFavored";
            star.Tooltip = "Click to REMOVE from favorites";
        }
        else
        {
            star.CssClass = "starNone";
            star.Tooltip = "Click to add to favorites";
        }
        numberOfFavorites.Text = _question.CountFavorites(null).ToString();
    }

    private void FillOutContent()
    {
        header.Text = _question.Header;
        whenPosted.Text = " [" + GetTime(_question.Created) + "]";
        body.Text = _question.BodyFormated;
        count.Text = _question.GetScore().ToString();
        askedBy.InnerHtml = _question.CreatedBy.FriendlyName + " - " + _question.CreatedBy.GetCreds() + " creds";
        askedBy.HRef = _question.CreatedBy.Username + ".user";
        Title = _question.Header;
    }

    protected void editQuestionBtn_Click(object sender, EventArgs e)
    {
        if (!editQuestion.Visible || editQuestion.Style["display"] == "none")
        {
            editQuestion.Visible = true;
            editTxt.Text = _question.Body;
            editHeader.Text = _question.Header;
            new EffectFadeIn(editQuestion, 500)
                .ChainThese(new EffectFocusAndSelect(editTxt))
                .Render();
            editQuestionBtn.Text = "Cancel edit";
        }
        else
        {
            new EffectFadeOut(editQuestion, 500).Render();
            editQuestionBtn.Text = "Edit";
        }
    }

    protected void saveEdit_Click(object sender, EventArgs e)
    {
        new EffectFadeOut(editQuestion, 500).Render();
        editQuestionBtn.Text = "Edit";
        _question.Body = editTxt.Text;
        _question.Header = editHeader.Text;
        _question.Save();
        body.Text = _question.BodyFormated;
        header.Text = _question.Header;
    }

    protected void deleteQuestion_Click(object sender, EventArgs e)
    {
        wndDelete.Visible = true;
        deleteBtn.Focus();
    }

    protected void changeOrdering_Click(object sender, EventArgs e)
    {
        if (OrderAnswersBy == QuizItem.OrderAnswersBy.MostVotes)
        {
            OrderAnswersBy = QuizItem.OrderAnswersBy.Newest;
            changeOrdering.Text = "Order by oldest [current - Newest]";
        }
        else if (OrderAnswersBy == QuizItem.OrderAnswersBy.Newest || OrderAnswersBy == QuizItem.OrderAnswersBy.Determine)
        {
            OrderAnswersBy = QuizItem.OrderAnswersBy.Oldest;
            changeOrdering.Text = "Order by most votes [current - Oldest]";
        }
        else if (OrderAnswersBy == QuizItem.OrderAnswersBy.Oldest)
        {
            OrderAnswersBy = QuizItem.OrderAnswersBy.MostVotes;
            changeOrdering.Text = "Order by newest [current - Most Votes]";
        }
        DataBindAnswers();
        new EffectRollUp(answersWrapper, 200)
            .ChainThese(new EffectRollDown(answersWrapper, 800))
            .Render();
    }

    protected void deleteBtn_Click(object sender, EventArgs e)
    {
        _question.Delete();
        AjaxManager.Instance.Redirect("~/");
    }

    private void SetCssClassIfCurrentOperatorHasVoted()
    {
        // Setting CSS classes of voters for QUESTION
        up.CssClass = "up";
        down.CssClass = "down";
        if (Operator.Current != null)
        {
            Vote hasVoted = Vote.FindOne(
                Expression.Eq("VotedBy", Operator.Current),
                Expression.Eq("QuizItem", _question));
            if (hasVoted != null)
            {
                if (hasVoted.Score > 0)
                    up.CssClass = "upVoted";
                else
                    down.CssClass = "downVoted";
            }
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        if (!IsPostBack)
        {
            DataBindAnswers();
        }
        base.OnLoad(e);
    }

    protected void btnSubmit_Click(object sender, EventArgs e)
    {
        // Saving question...
        QuizItem q = new QuizItem();
        q.Body = answerBody.Text;
        q.CreatedBy = Operator.Current;
        q.Header = "";
        q.Parent = _question;
        q.Save();

        // Binding grid again
        DataBindAnswers();

        new EffectHighlight(answersWrapper, 500).Render();
        answerBody.Text = "";
    }

    protected void ViewComments(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        foreach (System.Web.UI.Control idx in btn.Parent.Controls)
        {
            if (idx is Panel)
            {
                Panel tmp = idx as Panel;
                if (tmp.CssClass == "viewComments")
                {
                    int id = GetIdOfAnswer(btn);
                    tmp.Visible = true;
                    QuizItem answer = QuizItem.Find(id);

                    // Finding repeater
                    foreach (System.Web.UI.Control idxC in tmp.Controls)
                    {
                        if (idxC is System.Web.UI.WebControls.Repeater)
                        {
                            System.Web.UI.WebControls.Repeater rep = idxC as System.Web.UI.WebControls.Repeater;
                            rep.DataSource = answer.Children;
                            rep.DataBind();
                        }
                        else if (idxC is TextArea)
                        {
                            new EffectFadeIn(tmp, 200)
                                .ChainThese(new EffectFocusAndSelect(idxC as TextArea))
                                .Render();
                            (idxC as TextArea).Text = "write your comment here...";
                        }
                    }
                    tmp.ReRender();
                }
            }
        }
    }

    protected void SaveComment(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        int id = GetIdOfAnswer(btn.Parent);
        QuizItem q = QuizItem.Find(id);
        foreach (System.Web.UI.Control idx in btn.Parent.Controls)
        {
            if (idx is TextArea)
            {
                TextArea tmp = idx as TextArea;
                QuizItem n = new QuizItem();
                n.Body = tmp.Text;
                n.CreatedBy = Operator.Current;
                n.Parent = q;
                n.Save();
                q.Refresh();
                new EffectFadeOut((idx.Parent as Panel), 300).Render();
            }
        }
        foreach (System.Web.UI.Control idx in btn.Parent.Parent.Controls)
        {
            if (idx is LinkButton)
            {
                LinkButton viewComments = idx as LinkButton;
                if (viewComments.CssClass == "comments")
                {
                    viewComments.Text = "Comments [" + q.Children.Count + "]";
                }
            }
        }
    }

    private int GetIdOfAnswer(System.Web.UI.Control ctrl)
    {
        foreach (System.Web.UI.Control idx in ctrl.Parent.Controls)
        {
            if (idx is HiddenField)
            {
                return int.Parse((idx as HiddenField).Value);
            }
        }
        return -1;
    }

    private Label FindLabelForAnswer(System.Web.UI.Control ctrl)
    {
        foreach (System.Web.UI.Control idx in ctrl.Parent.Controls)
        {
            if (idx is Label)
            {
                return idx as Label;
            }
        }
        return null;
    }

    private LinkButton FindUpLinkButtonForAnswer(System.Web.UI.Control ctrl)
    {
        foreach (System.Web.UI.Control idx in ctrl.Parent.Controls)
        {
            if (idx is LinkButton)
            {
                return idx as LinkButton;
            }
        }
        return null;
    }

    private LinkButton FindDownLinkButtonForAnswer(System.Web.UI.Control ctrl)
    {
        bool first = true;
        foreach (System.Web.UI.Control idx in ctrl.Parent.Controls)
        {
            if (idx is LinkButton)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                return idx as LinkButton;
            }
        }
        return null;
    }

    protected void VoteAnswerUp(object sender, EventArgs e)
    {
        int idOfQuizItem = GetIdOfAnswer(sender as System.Web.UI.Control);

        // Saving will throw if you vote for your own question/answer...
        try
        {
            QuizItem item = QuizItem.Find(idOfQuizItem);
            Vote o = Vote.FindOne(
                Expression.Eq("VotedBy", Operator.Current),
                Expression.Eq("QuizItem", item),
                Expression.Eq("Score", 1));
            if (o != null)
            {
                o.Delete();
                FindUpLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "up";
                FindDownLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "down";
            }
            else
            {
                Vote v = new Vote();
                v.QuizItem = item;
                v.Score = 1;
                v.VotedBy = Operator.Current;
                v.Save();
                FindUpLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "upVoted";
                FindDownLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "down";
            }
            FindLabelForAnswer(sender as System.Web.UI.Control).Text = QuizItem.Find(idOfQuizItem).Score.ToString();
        }
        catch (Exception err)
        {
            ShowError(err.Message);
        }
    }

    private void ShowError(string error)
    {
        errorLabel.Text = error;
        errorLabel.Visible = true;
        errorLabel.Style["display"] = "none";
        new EffectFadeIn(errorLabel, 1000).Render();
        timerRemoveError.Enabled = true;
    }

    protected void timerRemoveError_Tick(object sender, EventArgs e)
    {
        new EffectFadeOut(errorLabel, 1000).Render();
        timerRemoveError.Enabled = false;
    }

    protected void VoteAnswerDown(object sender, EventArgs e)
    {
        int idOfQuizItem = GetIdOfAnswer(sender as System.Web.UI.Control);

        try
        {
            QuizItem item = QuizItem.Find(idOfQuizItem);
            Vote o = Vote.FindOne(
                Expression.Eq("VotedBy", Operator.Current),
                Expression.Eq("QuizItem", item),
                Expression.Eq("Score", -1));
            if (o != null)
            {
                o.Delete();
                FindUpLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "up";
                FindDownLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "down";
            }
            else
            {
                Vote v = new Vote();
                v.QuizItem = item;
                v.Score = -1;
                v.VotedBy = Operator.Current;
                v.Save();
                FindUpLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "up";
                FindDownLinkButtonForAnswer(sender as System.Web.UI.Control).CssClass = "downVoted";
            }
            FindLabelForAnswer(sender as System.Web.UI.Control).Text = QuizItem.Find(idOfQuizItem).Score.ToString();
        }
        catch (Exception err)
        {
            ShowError(err.Message);
        }
    }

    protected string GetUpCssClassIfVoted(int id)
    {
        QuizItem quiz = QuizItem.Find(id);
        Vote hasVoted = Vote.FindOne(
            Expression.Eq("VotedBy", Operator.Current),
            Expression.Eq("QuizItem", quiz),
            Expression.Eq("Score", 1));
        if (hasVoted != null)
            return "upVoted";
        return "up";
    }

    protected string GetDownCssClassIfVoted(int id)
    {
        QuizItem quiz = QuizItem.Find(id);
        Vote hasVoted = Vote.FindOne(
            Expression.Eq("VotedBy", Operator.Current),
            Expression.Eq("QuizItem", quiz),
            Expression.Eq("Score", -1));
        if (hasVoted != null)
            return "downVoted";
        return "down";
    }

    protected void up_Click(object sender, EventArgs e)
    {
        try
        {
            Vote o = Vote.FindOne(
                Expression.Eq("VotedBy", Operator.Current),
                Expression.Eq("QuizItem", _question),
                Expression.Eq("Score", 1));
            if (o != null)
            {
                // User has voted this BEFORE. Therefore removing old vote...
                o.Delete();
                count.Text = _question.GetScore().ToString();
                down.CssClass = "down";
                up.CssClass = "up";
                Highlight(up);
            }
            else
            {
                CreateVoteForCurrent(true);
            }
        }
        catch (Exception err)
        {
            ShowError(err.Message);
        }
    }

    protected void down_Click(object sender, EventArgs e)
    {
        try
        {
            Vote o = Vote.FindOne(
                Expression.Eq("VotedBy", Operator.Current),
                Expression.Eq("QuizItem", _question),
                Expression.Eq("Score", -1));
            if (o != null)
            {
                // User has voted this BEFORE. Therefore removing old vote...
                o.Delete();
                count.Text = _question.GetScore().ToString();
                down.CssClass = "down";
                up.CssClass = "up";
                Highlight(down);
            }
            else
            {
                CreateVoteForCurrent(false);
            }
        }
        catch (Exception err)
        {
            ShowError(err.Message);
        }
    }

    private void CreateVoteForCurrent(bool isUp)
    {
        Vote v = new Vote();
        v.Score = isUp ? 1 : -1;
        v.VotedBy = Operator.Current;
        v.QuizItem = _question;
        v.Save();
        count.Text = _question.GetScore().ToString();
        down.CssClass = isUp ? "down" : "downVoted";
        up.CssClass = isUp ? "upVoted" : "up";
        Highlight(isUp ? up : down);
    }

    private void Highlight(LinkButton ctrl)
    {
        new EffectHighlight(ctrl, 500).Render();
    }

    private void GetQuestion()
    {
        string id = Request["id"];
        if (string.IsNullOrEmpty(id))
            Response.Redirect("~/", true);
        _question = QuizItem.FindOne(Expression.Eq("Url", id + ".quiz"));
        if (_question == null)
            Response.Redirect("~/", true);

        // Checking to see if we should increase the "view count" of this question
        if (!IsPostBack)
        {
            _question.IncreaseViewCount();
        }
    }

    protected string GetTime(DateTime time)
    {
        return TimeFormatter.Format(time);
    }
}
